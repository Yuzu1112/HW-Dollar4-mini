Option Strict On
Option Explicit On

Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' aescrypt2 格式（Christophe Devine 的 aescrypt2 1.0）的纯托管实现，
''' 取代原工程通过外置 aescrypt2.exe 进程完成加解密的方式（原 AesCrypt2 见 MainUI.vb）。
''' <para>
''' 数据布局：<c>IV(16) + AES-256-CBC 密文(16 的整数倍) + HMAC-SHA256(32)</c>。
''' 密钥派生：<c>digest = SHA256(digest + userKey)</c>，初值为 IV 补零到 32 字节，
''' 迭代 <b>8192</b> 轮；该 digest 同时用作 AES-256 密钥与 HMAC 密钥，HMAC 覆盖密文段。
''' </para>
''' <para>
''' 密钥语义：<b>传入字节原样使用，不做任何 hex 解码</b>。原工程传的是
''' <c>Encoding.UTF8.GetBytes("hex:13395537D2730554A176799F6D56A239")</c>，
''' 含 <c>hex:</c> 前缀共 36 字节；改用 hex 解码后的 16 字节或去掉前缀的 32 字节均解不出正确结果。
''' </para>
''' <para>
''' 空正文：原版 aescrypt2.exe 对 0 字节输入产出 <b>48</b> 字节
''' （IV + 0 个密文块 + HMAC），能原样解回 0 字节。本实现遵循该行为，
''' 解密侧同样接受 48 字节输入。IV 末字节低 4 位为 0 时存在二义性
''' （0 字节 / 16 的整数倍），必须结合密文段长度判定，不能只看 IV。
''' </para>
''' </summary>
Public NotInheritable Class HwAescrypt2

    ''' <summary>IV 长度，同时也是 AES 块大小。</summary>
    Private Const IvLength As Integer = 16

    ''' <summary>HMAC-SHA256 输出长度。</summary>
    Private Const HmacLength As Integer = 32

    ''' <summary>密钥派生迭代轮数（与 aescrypt2 1.0 一致）。</summary>
    Private Const DerivationRounds As Integer = 8192

    Private Sub New()
    End Sub

    ''' <summary>
    ''' 加密为 aescrypt2 格式。IV 由「当前时间 + 数据长度 + 文件名」派生，
    ''' 因此同一份数据每次加密结果不同，不可直接比对密文。
    ''' </summary>
    ''' <param name="plain">明文，可为 0 字节。</param>
    ''' <param name="userKey">用户密钥，原样使用，不做 hex 解码。</param>
    ''' <param name="filename">参与 IV 派生的文件名，仅用于增加熵，可为空。</param>
    Public Shared Function Encrypt(plain As Byte(), userKey As Byte(), Optional filename As String = "") As Byte()
        If plain Is Nothing Then plain = New Byte() {}
        If userKey Is Nothing Then userKey = New Byte() {}
        Return EncryptWithIv(plain, userKey, BuildIv(plain.Length, filename))
    End Function

    ''' <summary>
    ''' 用指定的 IV 加密，供交叉验证与需要固定 IV 的场景使用。
    ''' IV 的第 16 字节低 4 位会被强制改写为「明文长度 mod 16」，这是格式约定，无法保留原值。
    ''' </summary>
    Public Shared Function EncryptWithIv(plain As Byte(), userKey As Byte(), iv As Byte()) As Byte()
        If plain Is Nothing Then plain = New Byte() {}
        If userKey Is Nothing Then userKey = New Byte() {}
        If iv Is Nothing OrElse iv.Length < IvLength Then
            Throw New ArgumentException("IV 长度不足 16 字节。", "iv")
        End If

        Dim workIv As Byte() = New Byte(IvLength - 1) {}
        Array.Copy(iv, workIv, IvLength)
        workIv(IvLength - 1) = CByte((workIv(IvLength - 1) And &HF0) Or (plain.Length And &HF))

        Dim aesKey As Byte() = DeriveKey(workIv, userKey)

        ' 密文块数向上取整；0 字节明文对应 0 个密文块（与原版 exe 一致，不补整块）。
        Dim cipherLength As Integer = ((plain.Length + IvLength - 1) \ IvLength) * IvLength
        Dim cipher As Byte()

        If cipherLength = 0 Then
            cipher = New Byte() {}
        Else
            Dim padded As Byte() = New Byte(cipherLength - 1) {}
            Array.Copy(plain, padded, plain.Length)

            Using aes As Aes = Aes.Create()
                aes.Key = aesKey
                aes.IV = workIv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.None
                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    cipher = encryptor.TransformFinalBlock(padded, 0, padded.Length)
                End Using
            End Using
        End If

        Dim mac As Byte()
        Using hmac As New HMACSHA256(aesKey)
            mac = hmac.ComputeHash(cipher)
        End Using

        Dim output As Byte() = New Byte(IvLength + cipherLength + HmacLength - 1) {}
        Array.Copy(workIv, 0, output, 0, IvLength)
        Array.Copy(cipher, 0, output, IvLength, cipherLength)
        Array.Copy(mac, 0, output, IvLength + cipherLength, HmacLength)
        Return output
    End Function

    ''' <summary>
    ''' 解密 aescrypt2 数据。成功返回明文（可能为 0 字节数组），
    ''' 长度非法或 HMAC 校验不通过时返回 <c>Nothing</c>。
    ''' </summary>
    Public Shared Function TryDecrypt(input As Byte(), userKey As Byte()) As Byte()
        If input Is Nothing Then Return Nothing
        Return TryDecrypt(input, 0, input.Length, userKey)
    End Function

    ''' <summary>解密 aescrypt2 数据（带偏移与长度，密文段零拷贝）。</summary>
    Public Shared Function TryDecrypt(input As Byte(), offset As Integer, length As Integer, userKey As Byte()) As Byte()
        If input Is Nothing OrElse userKey Is Nothing Then Return Nothing
        If offset < 0 OrElse length < 0 Then Return Nothing
        If offset + length > input.Length Then Return Nothing

        ' 最小合法长度是 IV + HMAC（密文段可为 0），即原版 exe 的空输入产物 48 字节。
        If length < IvLength + HmacLength Then Return Nothing
        If (length And &HF) <> 0 Then Return Nothing

        Dim cipherLength As Integer = length - IvLength - HmacLength

        Dim iv As Byte() = New Byte(IvLength - 1) {}
        Array.Copy(input, offset, iv, 0, IvLength)
        Dim lastN As Integer = iv(IvLength - 1) And &HF

        Dim aesKey As Byte() = DeriveKey(iv, userKey)

        ' HMAC 覆盖密文段，键为派生出的 AES 密钥。
        Dim expected As Byte() = New Byte(HmacLength - 1) {}
        Array.Copy(input, offset + IvLength + cipherLength, expected, 0, HmacLength)

        Dim actual As Byte() = Nothing
        If cipherLength = 0 Then
            Using hmac As New HMACSHA256(aesKey)
                actual = hmac.ComputeHash(New Byte() {})
            End Using
        Else
            Dim segment As Byte() = New Byte(cipherLength - 1) {}
            Array.Copy(input, offset + IvLength, segment, 0, cipherLength)
            Using hmac As New HMACSHA256(aesKey)
                actual = hmac.ComputeHash(segment)
            End Using
        End If

        If Not FixedTimeEquals(actual, expected) Then Return Nothing

        If cipherLength = 0 Then Return New Byte() {}

        Dim buffer As Byte() = New Byte(cipherLength - 1) {}
        Array.Copy(input, offset + IvLength, buffer, 0, cipherLength)

        Try
            Using aes As Aes = Aes.Create()
                aes.Key = aesKey
                aes.IV = iv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.None
                Using decryptor As ICryptoTransform = aes.CreateDecryptor()
                    buffer = decryptor.TransformFinalBlock(buffer, 0, buffer.Length)
                End Using
            End Using
        Catch ex As CryptographicException
            Return Nothing
        End Try

        ' 明文真实长度：末块有效字节数记在 IV 末字节低 4 位，为 0 表示末块恰好整块。
        Dim plainLength As Integer
        If lastN = 0 Then
            plainLength = cipherLength
        Else
            plainLength = cipherLength - IvLength + lastN
        End If

        If plainLength <= 0 OrElse plainLength > cipherLength Then Return Nothing

        Dim result As Byte() = New Byte(plainLength - 1) {}
        Array.Copy(buffer, result, plainLength)
        Return result
    End Function

    ''' <summary>派生密钥：<c>digest = SHA256(digest + userKey)</c> 迭代 8192 轮。</summary>
    Private Shared Function DeriveKey(iv As Byte(), userKey As Byte()) As Byte()
        Dim buffer As Byte() = New Byte(31 + userKey.Length) {}
        Dim digest As Byte() = New Byte(31) {}
        Array.Copy(iv, digest, Math.Min(iv.Length, IvLength))

        Using sha As SHA256 = SHA256.Create()
            For round As Integer = 0 To DerivationRounds - 1
                Array.Copy(digest, 0, buffer, 0, 32)
                Array.Copy(userKey, 0, buffer, 32, userKey.Length)
                digest = sha.ComputeHash(buffer)
            Next
        End Using

        Return digest
    End Function

    ''' <summary>构造 IV：SHA256(时间 + 长度 + 文件名) 取前 16 字节，末字节低 4 位写入长度 mod 16。</summary>
    Private Shared Function BuildIv(length As Integer, filename As String) As Byte()
        Dim name As Byte() = Encoding.UTF8.GetBytes(If(filename, String.Empty))
        Dim seed As Byte() = New Byte(15 + name.Length) {}
        Dim seconds As Long = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        Dim size As Long = length

        For i As Integer = 0 To 7
            seed(i) = CByte((seconds >> (i * 8)) And &HFFL)
            seed(i + 8) = CByte((size >> (i * 8)) And &HFFL)
        Next
        Array.Copy(name, 0, seed, 16, name.Length)

        Dim digest As Byte() = ComputeSha256(seed)
        Dim iv As Byte() = New Byte(IvLength - 1) {}
        Array.Copy(digest, iv, IvLength)
        iv(IvLength - 1) = CByte((iv(IvLength - 1) And &HF0) Or (length And &HF))
        Return iv
    End Function

    ''' <summary>
    ''' SHA256 单次摘要。注意：本工程为 VB，标识符不区分大小写，
    ''' 函数名不能取作 Sha256，否则会与类型 SHA256 冲突。
    ''' </summary>
    Private Shared Function ComputeSha256(data As Byte()) As Byte()
        Using hasher As SHA256 = SHA256.Create()
            Return hasher.ComputeHash(data)
        End Using
    End Function

    ''' <summary>常量时间比较，避免按字节提前返回带来的时序泄漏（.NET Framework 4.7.2 无 FixedTimeEquals）。</summary>
    Private Shared Function FixedTimeEquals(left As Byte(), right As Byte()) As Boolean
        If left Is Nothing OrElse right Is Nothing Then Return False
        If left.Length <> right.Length Then Return False

        Dim diff As Integer = 0
        For i As Integer = 0 To left.Length - 1
            diff = diff Or (CInt(left(i)) Xor CInt(right(i)))
        Next
        Return diff = 0
    End Function

End Class
