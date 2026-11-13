Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' 华为 $3 / $4 所用的 AES-256-GCM 加解密封装，走系统 CNG（bcrypt.dll），
''' 取代原工程对 libmbedtls.dll 的依赖（原调用见 MainUI.vb 的 HW_D4Decrypt / HW_D4Encrypt）。
''' <para>
''' 与 C 侧 <c>libmbedtls_gcm_crypt_and_tag_pbkdf2</c> 严格对齐的参数：
''' PBKDF2-HMAC-SHA256 派生 32 字节密钥、<b>迭代 1 次</b>（华为的设定，不是 8192）、
''' AES-256-GCM、<b>nonce 16 字节</b>、tag 16 字节、AAD 为空、密文与明文等长。
''' </para>
''' <para>
''' 为什么不用 .NET 原生 <c>AesGcm</c>：它只接受 12 字节 nonce，
''' 而华为 $4 用的是 16 字节 IV，会抛 ArgumentException；.NET Framework 4.7.2 更没有该类型。
''' CNG 对 nonce 长度无此限制。
''' </para>
''' </summary>
Public NotInheritable Class HwGcm

    ''' <summary>派生密钥长度（AES-256）。</summary>
    Private Const KeyLength As Integer = 32

    ''' <summary>认证标签长度。</summary>
    Private Const TagLength As Integer = 16

    ''' <summary>BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO 的结构版本号。</summary>
    Private Const AuthInfoVersion As Integer = 1

    Private Const StatusSuccess As Integer = 0

    ''' <summary>
    ''' 对应 C 的 BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO。
    ''' 字段顺序与 windows.h 一致；结构尺寸在运行时用 Marshal.SizeOf 取得，
    ''' 因为 Debug 为 AnyCPU、Release 为 x64，两种位数都要能正确工作。
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Private Structure AuthCipherModeInfo
        Public cbSize As Integer
        Public dwInfoVersion As Integer
        Public pbNonce As IntPtr
        Public cbNonce As Integer
        Public pbAuthData As IntPtr
        Public cbAuthData As Integer
        Public pbTag As IntPtr
        Public cbTag As Integer
        Public pbMacContext As IntPtr
        Public cbMacContext As Integer
        Public cbAAD As Integer
        Public cbData As Long
        Public dwFlags As Integer
    End Structure

    ' CharSet.Unicode 必须显式指定：CNG 的字符串参数是 UTF-16，
    ' 缺省按 ANSI 传入会让 BCryptOpenAlgorithmProvider 返回 0xC0000225（STATUS_NOT_FOUND），
    ' 表现为「系统不支持 GCM」，实际只是编码不对。
    <DllImport("bcrypt.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function BCryptOpenAlgorithmProvider(ByRef phAlgorithm As IntPtr, pszAlgId As String, pszImplementation As String, dwFlags As Integer) As Integer
    End Function

    <DllImport("bcrypt.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function BCryptSetProperty(hObject As IntPtr, pszProperty As String, pbInput As Byte(), cbInput As Integer, dwFlags As Integer) As Integer
    End Function

    <DllImport("bcrypt.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function BCryptGenerateSymmetricKey(hAlgorithm As IntPtr, ByRef phKey As IntPtr, pbKeyObject As Byte(), cbKeyObject As Integer, pbSecret As Byte(), cbSecret As Integer, dwFlags As Integer) As Integer
    End Function

    <DllImport("bcrypt.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function BCryptEncrypt(hKey As IntPtr, pbInput As Byte(), cbInput As Integer, ByRef pPaddingInfo As AuthCipherModeInfo, pbIV As Byte(), cbIV As Integer, pbOutput As Byte(), cbOutput As Integer, ByRef pcbResult As Integer, dwFlags As Integer) As Integer
    End Function

    <DllImport("bcrypt.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function BCryptDecrypt(hKey As IntPtr, pbInput As Byte(), cbInput As Integer, ByRef pPaddingInfo As AuthCipherModeInfo, pbIV As Byte(), cbIV As Integer, pbOutput As Byte(), cbOutput As Integer, ByRef pcbResult As Integer, dwFlags As Integer) As Integer
    End Function

    <DllImport("bcrypt.dll")>
    Private Shared Function BCryptDestroyKey(hKey As IntPtr) As Integer
    End Function

    <DllImport("bcrypt.dll")>
    Private Shared Function BCryptCloseAlgorithmProvider(hAlgorithm As IntPtr, dwFlags As Integer) As Integer
    End Function

    Private Sub New()
    End Sub

    ''' <summary>
    ''' 用 PBKDF2 派生密钥后加密。等价于 C 的 <c>libmbedtls_gcm_crypt_and_tag_pbkdf2</c>。
    ''' </summary>
    ''' <param name="password">口令字节（原样使用）。</param>
    ''' <param name="salt">PBKDF2 盐，16 字节。</param>
    ''' <param name="iv">GCM nonce，16 字节。</param>
    ''' <param name="plain">明文，可为 0 字节。</param>
    ''' <param name="tag">输出：16 字节认证标签。</param>
    ''' <param name="iterations">PBKDF2 迭代次数，华为固定为 1。</param>
    ''' <returns>与明文等长的密文。</returns>
    Public Shared Function EncryptPbkdf2(password As Byte(), salt As Byte(), iv As Byte(), plain As Byte(), ByRef tag As Byte(), Optional iterations As Integer = 1) As Byte()
        Dim key As Byte() = DerivePbkdf2(password, salt, iterations)
        Return EncryptCore(key, iv, plain, tag)
    End Function

    ''' <summary>
    ''' 用 PBKDF2 派生密钥后解密并校验认证标签。等价于 C 的
    ''' <c>libmbedtls_gcm_auth_decrypt_pbkdf2</c>。标签不匹配或参数非法时返回 <c>Nothing</c>。
    ''' </summary>
    Public Shared Function DecryptPbkdf2(password As Byte(), salt As Byte(), iv As Byte(), cipher As Byte(), tag As Byte(), Optional iterations As Integer = 1) As Byte()
        If tag Is Nothing OrElse tag.Length <> TagLength Then Return Nothing
        Dim key As Byte() = DerivePbkdf2(password, salt, iterations)
        Return DecryptCore(key, iv, cipher, tag)
    End Function

    ''' <summary>直接用已派生的 32 字节密钥加密（供需要自行管理口令派生的调用方使用）。</summary>
    Public Shared Function EncryptWithKey(key As Byte(), iv As Byte(), plain As Byte(), ByRef tag As Byte()) As Byte()
        Return EncryptCore(key, iv, plain, tag)
    End Function

    ''' <summary>直接用已派生的 32 字节密钥解密。</summary>
    Public Shared Function DecryptWithKey(key As Byte(), iv As Byte(), cipher As Byte(), tag As Byte()) As Byte()
        If tag Is Nothing OrElse tag.Length <> TagLength Then Return Nothing
        Return DecryptCore(key, iv, cipher, tag)
    End Function

    Private Shared Function EncryptCore(key As Byte(), iv As Byte(), plain As Byte(), ByRef tag As Byte()) As Byte()
        If key Is Nothing OrElse key.Length <> KeyLength Then
            Throw New ArgumentException("GCM 密钥必须为 32 字节。", "key")
        End If
        If iv Is Nothing OrElse iv.Length = 0 Then
            Throw New ArgumentException("GCM nonce 不能为空。", "iv")
        End If
        If plain Is Nothing Then plain = New Byte() {}

        Dim algorithm As IntPtr = IntPtr.Zero
        Dim keyHandle As IntPtr = IntPtr.Zero
        Dim nonceHandle As GCHandle = Nothing
        Dim tagHandle As GCHandle = Nothing
        Dim tagBuffer As Byte() = New Byte(TagLength - 1) {}

        Try
            CreateAlgorithmAndKey(key, algorithm, keyHandle)

            nonceHandle = GCHandle.Alloc(iv, GCHandleType.Pinned)
            tagHandle = GCHandle.Alloc(tagBuffer, GCHandleType.Pinned)

            Dim info As AuthCipherModeInfo = BuildInfo(iv, nonceHandle, tagHandle)

            Dim output As Byte() = New Byte(Math.Max(plain.Length, 1) - 1) {}
            Dim written As Integer = 0

            Dim status As Integer = BCryptEncrypt(
                keyHandle,
                If(plain.Length = 0, Nothing, plain), plain.Length,
                info,
                iv, iv.Length,
                If(plain.Length = 0, Nothing, output), If(plain.Length = 0, 0, output.Length),
                written, 0)

            ThrowOnError(status, "BCryptEncrypt")

            tag = tagBuffer
            If written = 0 Then Return New Byte() {}

            Dim cipher As Byte() = New Byte(written - 1) {}
            Array.Copy(output, cipher, written)
            Return cipher
        Finally
            If tagHandle.IsAllocated Then tagHandle.Free()
            If nonceHandle.IsAllocated Then nonceHandle.Free()
            If keyHandle <> IntPtr.Zero Then BCryptDestroyKey(keyHandle)
            If algorithm <> IntPtr.Zero Then BCryptCloseAlgorithmProvider(algorithm, 0)
        End Try
    End Function

    Private Shared Function DecryptCore(key As Byte(), iv As Byte(), cipher As Byte(), tag As Byte()) As Byte()
        If key Is Nothing OrElse key.Length <> KeyLength Then Return Nothing
        If iv Is Nothing OrElse iv.Length = 0 Then Return Nothing
        If cipher Is Nothing Then cipher = New Byte() {}

        Dim algorithm As IntPtr = IntPtr.Zero
        Dim keyHandle As IntPtr = IntPtr.Zero
        Dim nonceHandle As GCHandle = Nothing
        Dim tagHandle As GCHandle = Nothing

        Try
            CreateAlgorithmAndKey(key, algorithm, keyHandle)

            nonceHandle = GCHandle.Alloc(iv, GCHandleType.Pinned)
            tagHandle = GCHandle.Alloc(tag, GCHandleType.Pinned)

            Dim info As AuthCipherModeInfo = BuildInfo(iv, nonceHandle, tagHandle)

            Dim output As Byte() = New Byte(Math.Max(cipher.Length, 1) - 1) {}
            Dim written As Integer = 0

            Dim status As Integer = BCryptDecrypt(
                keyHandle,
                If(cipher.Length = 0, Nothing, cipher), cipher.Length,
                info,
                iv, iv.Length,
                If(cipher.Length = 0, Nothing, output), If(cipher.Length = 0, 0, output.Length),
                written, 0)

            ' 非 0 包含 STATUS_AUTH_TAG_MISMATCH（0xC000A002），视为校验失败而非异常。
            If status <> StatusSuccess Then Return Nothing
            If written = 0 Then Return New Byte() {}

            Dim plain As Byte() = New Byte(written - 1) {}
            Array.Copy(output, plain, written)
            Return plain
        Finally
            If tagHandle.IsAllocated Then tagHandle.Free()
            If nonceHandle.IsAllocated Then nonceHandle.Free()
            If keyHandle <> IntPtr.Zero Then BCryptDestroyKey(keyHandle)
            If algorithm <> IntPtr.Zero Then BCryptCloseAlgorithmProvider(algorithm, 0)
        End Try
    End Function

    ''' <summary>打开 AES 算法提供程序、切到 GCM 链模式、导入对称密钥。</summary>
    Private Shared Sub CreateAlgorithmAndKey(key As Byte(), ByRef algorithm As IntPtr, ByRef keyHandle As IntPtr)
        ThrowOnError(BCryptOpenAlgorithmProvider(algorithm, "AES", Nothing, 0), "BCryptOpenAlgorithmProvider")

        Dim mode As Byte() = Encoding.Unicode.GetBytes("ChainingModeGCM" & ChrW(0))
        ThrowOnError(BCryptSetProperty(algorithm, "ChainingMode", mode, mode.Length, 0), "BCryptSetProperty(ChainingMode)")

        ThrowOnError(BCryptGenerateSymmetricKey(algorithm, keyHandle, Nothing, 0, key, key.Length, 0), "BCryptGenerateSymmetricKey")
    End Sub

    Private Shared Function BuildInfo(iv As Byte(), nonceHandle As GCHandle, tagHandle As GCHandle) As AuthCipherModeInfo
        Dim info As New AuthCipherModeInfo()
        info.cbSize = Marshal.SizeOf(GetType(AuthCipherModeInfo))
        info.dwInfoVersion = AuthInfoVersion
        info.pbNonce = nonceHandle.AddrOfPinnedObject()
        info.cbNonce = iv.Length
        info.pbAuthData = IntPtr.Zero
        info.cbAuthData = 0
        info.pbTag = tagHandle.AddrOfPinnedObject()
        info.cbTag = TagLength
        Return info
    End Function

    ''' <summary>
    ''' PBKDF2-HMAC-SHA256，可指定输出长度。
    ''' 华为在「人工设的口令」场景用 iterations=10000、length=16，再把结果转成 hex 当二级口令。
    ''' </summary>
    Public Shared Function Pbkdf2(password As Byte(), salt As Byte(), iterations As Integer, length As Integer) As Byte()
        If password Is Nothing Then password = New Byte() {}
        If salt Is Nothing Then salt = New Byte() {}
        If iterations < 1 Then iterations = 1
        If length < 1 Then length = KeyLength
        Using kdf As New Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)
            Return kdf.GetBytes(length)
        End Using
    End Function

    ''' <summary>PBKDF2-HMAC-SHA256，华为侧 GCM 密钥派生的迭代次数为 1。</summary>
    Private Shared Function DerivePbkdf2(password As Byte(), salt As Byte(), iterations As Integer) As Byte()
        If password Is Nothing Then password = New Byte() {}
        If salt Is Nothing Then salt = New Byte() {}
        If iterations < 1 Then iterations = 1

        Using kdf As New Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)
            Return kdf.GetBytes(KeyLength)
        End Using
    End Function

    Private Shared Sub ThrowOnError(status As Integer, operation As String)
        If status = StatusSuccess Then Return
        Throw New CryptographicException(String.Format("{0} 失败，NTSTATUS = 0x{1:X8}", operation, status))
    End Sub

End Class
