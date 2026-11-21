namespace Freenaute.Freebox.Mcp.Security;

internal sealed partial class WindowsCredentialStorage : IFreeboxCredentialStorage
{
    public bool IsAvailable => true;

    public string Kind => "windows-credential-manager";

    public ValueTask<FreeboxAppCredentials?> GetAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string targetName = CredentialStoreKey.For(id);

        if (!Native.CredRead(targetName, Native.CredTypeGeneric, 0, out IntPtr credentialPtr))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error == Native.ErrorNotFound)
            {
                return ValueTask.FromResult<FreeboxAppCredentials?>(null);
            }

            throw new Win32Exception(error, "Windows Credential Manager could not read the Freebox credential.");
        }

        try
        {
            Native.Credential credential = Marshal.PtrToStructure<Native.Credential>(credentialPtr);
            if (credential.CredentialBlobSize > int.MaxValue || credential.CredentialBlob == IntPtr.Zero)
            {
                throw new InvalidDataException("Windows Credential Manager returned an invalid credential blob.");
            }

            byte[] json = new byte[credential.CredentialBlobSize];
            try
            {
                Marshal.Copy(credential.CredentialBlob, json, 0, json.Length);
                return ValueTask.FromResult(CredentialPayloadCodec.Deserialize(json));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(json);
            }
        }
        finally
        {
            Native.CredFree(credentialPtr);
        }
    }

    public unsafe ValueTask PutAsync(
        FreeboxCredentialId id,
        FreeboxAppCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] json = CredentialPayloadCodec.Serialize(credentials);
        try
        {
            fixed (byte* blob = json)
            fixed (char* targetName = CredentialStoreKey.For(id))
            {
                var credential = new Native.Credential
                {
                    Type = Native.CredTypeGeneric,
                    TargetName = targetName,
                    CredentialBlobSize = (uint)json.Length,
                    CredentialBlob = (IntPtr)blob,
                    Persist = Native.CredPersistLocalMachine
                };

                if (!Native.CredWrite(&credential, 0))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(),
                        "Windows Credential Manager could not persist the Freebox credential.");
                }
            }

            return ValueTask.CompletedTask;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    public ValueTask DeleteAsync(
        FreeboxCredentialId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Native.CredDelete(CredentialStoreKey.For(id), Native.CredTypeGeneric, 0))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != Native.ErrorNotFound)
            {
                throw new Win32Exception(error,
                    "Windows Credential Manager could not delete the Freebox credential.");
            }
        }

        return ValueTask.CompletedTask;
    }

    private static partial class Native
    {
        internal const uint CredTypeGeneric = 1;
        internal const uint CredPersistLocalMachine = 2;
        internal const int ErrorNotFound = 1168;

        [LibraryImport("advapi32.dll", EntryPoint = "CredReadW", StringMarshalling = StringMarshalling.Utf16,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CredRead(string targetName, uint type, uint flags, out IntPtr credential);

        [LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CredWrite(Credential* credential, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", StringMarshalling = StringMarshalling.Utf16,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CredDelete(string targetName, uint type, uint flags);

        [LibraryImport("advapi32.dll", EntryPoint = "CredFree")]
        internal static partial void CredFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct Credential
        {
            public uint Flags;
            public uint Type;
            public char* TargetName;
            public char* Comment;
            public FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public char* TargetAlias;
            public char* UserName;
        }
    }
}
