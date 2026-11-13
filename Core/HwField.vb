' $1 / $2 / $3 / $4 四种字段串的加解密。
'
' 四者的形态差异：
'   $1  ECB 逐块，每 16 字节密文后面跟 4 个字符的轮密钥提示，无 IV
'   $2  CBC 整体，末尾追加 20 字符 IV，整体再 base93 编码
'   $3  CBC 整体，头部 4 块为固定头（含 magic 20220323 / Domain / Key / IV / Salt / 迭代次数）
'   $4  GCM，Base64 承载，头 64 字节含 Domain / Key / IV / Tag / Salt
'
' 所有 base93 编解码走 HwBytes.HW_BytesToStr / HW_StrToBytes。
Imports System
Imports System.Collections.Generic
Imports System.Security.Cryptography
Imports System.Text

Public Module HwField

    ' ---------- 底层 AES / PBKDF2 ----------

    Public Function DecryptAES(Input As Byte(), Key As Byte(), IV As Byte()) As Byte()
        Dim a As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        a.BlockSize = 128
        a.KeySize = 256
        a.Key = Key
        a.IV = IV
        a.Mode = CipherMode.CBC
        a.Padding = PaddingMode.Zeros
        Return a.CreateDecryptor().TransformFinalBlock(Input, 0, Input.Length)
    End Function

    Public Function EncryptAES(Input As Byte(), Key As Byte(), IV As Byte()) As Byte()
        Dim a As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        a.BlockSize = 128
        a.KeySize = 256
        a.Key = Key
        a.IV = IV
        a.Mode = CipherMode.CBC
        a.Padding = PaddingMode.Zeros
        Return a.CreateEncryptor().TransformFinalBlock(Input, 0, Input.Length)
    End Function

    Public Function PKCS5_PBKDF2_HMAC(password As Byte(), salt As Byte(), iteration_count As Integer, key_length As Integer) As Byte()
        Using d As New Rfc2898DeriveBytes(password, salt, iteration_count, HashAlgorithmName.SHA256)
            Return d.GetBytes(key_length)
        End Using
    End Function

    ' ---------- $1 ----------

    Public Function HW_D1GetAESKey(param1 As String) As Byte()
        Dim k As Byte() = DirectCast(HwKeys.HW_D1_Key.Clone(), Byte())
        Dim s As Byte() = Encoding.UTF8.GetBytes(param1)
        For i As Integer = s.Length - 4 To s.Length - 1
            If s(i) = 126 Then
                s(i) = 30
            Else
                s(i) = CByte(s(i) - 33)
            End If
        Next
        k(11) = s(s.Length - 4)
        k(17) = s(s.Length - 3)
        k(23) = s(s.Length - 2)
        k(29) = s(s.Length - 1)
        Return k
    End Function

    Public Function HW_D1Decrypt(param1 As String) As String
        Dim body As String = HwBytes.UnescapeXmlText(param1.Substring(2, param1.Length - 3))
        Dim n As Integer = CInt(Math.Floor(body.Length / 24))
        Dim parts As New List(Of String)
        For i As Integer = 0 To n - 1
            parts.Add(body.Substring(24 * i, 24))
        Next
        Dim out As New List(Of Byte)()
        Dim a As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        a.BlockSize = 128
        a.KeySize = 256
        a.Mode = CipherMode.ECB
        a.Padding = PaddingMode.None
        For i As Integer = 0 To parts.Count - 1
            a.Key = HW_D1GetAESKey(parts(i))
            out.AddRange(a.CreateDecryptor().TransformFinalBlock(HwBytes.HW_StrToBytes(parts(i)), 0, 16))
        Next
        Return Encoding.UTF8.GetString(out.ToArray())
    End Function

    Public Function HW_D1Encrypt(param1 As String, Optional escapeXml As Boolean = False) As String
        Dim data As Byte() = Encoding.UTF8.GetBytes(param1)
        Dim blocks As Integer = CInt(Math.Ceiling(data.Length / 16))
        If blocks = 0 Then blocks = 1
        Dim rnd As New Random
        Dim iv As Byte() = New Byte(15) {}
        Dim sb As New StringBuilder
        Dim a As System.Security.Cryptography.Aes = System.Security.Cryptography.Aes.Create("AES")
        a.BlockSize = 128
        a.KeySize = 256
        a.Mode = CipherMode.ECB
        a.Padding = PaddingMode.None
        For b As Integer = 0 To blocks - 1
            Dim plain(15) As Byte
            Dim remain As Integer = data.Length - 16 * b
            Dim take As Integer = If(remain > 16, 16, remain)
            If take > 0 Then Array.Copy(data, 16 * b, plain, 0, take)
            rnd.NextBytes(iv)
            Dim hint As String = HwBytes.HW_BytesToStr(iv)
            a.Key = HW_D1GetAESKey(hint)
            sb.Append(HwBytes.HW_BytesToStr(a.CreateEncryptor().TransformFinalBlock(plain, 0, 16)))
            sb.Append(hint.Substring(hint.Length - 4, 4))
        Next
        Dim r As String = "$1" & sb.ToString() & "$"
        If escapeXml Then r = HwBytes.EscapeXmlText(r)
        Return r
    End Function

    ' ---------- $2 ----------

    Public Function HW_D2GetAESCBCKey() As Byte()
        Dim salt As String = HwKeys.D2SaltFor(HwCtx.D2KeyType)
        Dim buf(HwKeys.HW_D2_Key.Length + salt.Length - 1) As Byte
        Array.Copy(HwKeys.HW_D2_Key, buf, HwKeys.HW_D2_Key.Length)
        Array.Copy(Encoding.UTF8.GetBytes(salt), 0, buf, HwKeys.HW_D2_Key.Length, salt.Length)
        Return New SHA256CryptoServiceProvider().ComputeHash(buf)
    End Function

    Public Function HW_D2Decrypt(param1 As String) As String
        Dim body As String = HwBytes.UnescapeXmlText(param1.Substring(2, param1.Length - 3))
        Dim n As Integer = CInt(Math.Floor(body.Length / 20))
        Dim parts As New List(Of String)
        For i As Integer = 0 To n - 1
            parts.Add(body.Substring(20 * i, 20))
        Next
        Dim cipher((parts.Count - 1) * 16 - 1) As Byte
        For i As Integer = 0 To parts.Count - 2
            Array.Copy(HwBytes.HW_StrToBytes(parts(i)), 0, cipher, i * 16, 16)
        Next
        Return Encoding.UTF8.GetString(DecryptAES(cipher, HW_D2GetAESCBCKey(), HwBytes.HW_StrToBytes(parts(parts.Count - 1))))
    End Function

    Public Function HW_D2Encrypt(param1 As String, Optional escapeXml As Boolean = False) As String
        Dim rnd As New Random
        Dim iv As Byte() = New Byte(15) {}
        rnd.NextBytes(iv)
        Dim cipher As Byte() = EncryptAES(Encoding.UTF8.GetBytes(param1), HW_D2GetAESCBCKey(), iv)
        Dim blocks As Integer = CInt(Math.Floor(cipher.Length / 16))
        Dim sb As New StringBuilder
        For b As Integer = 0 To blocks - 1
            Dim one(15) As Byte
            Array.Copy(cipher, b * 16, one, 0, 16)
            sb.Append(HwBytes.HW_BytesToStr(one))
        Next
        Dim r As String = "$2" & sb.ToString() & HwBytes.HW_BytesToStr(iv) & "$"
        If escapeXml Then r = HwBytes.EscapeXmlText(r)
        Return r
    End Function

    ' ---------- $3 ----------

    ' 用给定主密钥解 $3。blockCount 为总块数（含 4 块固定头），checksum 表示尾部是否有 2 块校验。
    Public Function D3DecryptWith(master As Byte(), data As Byte(), blockCount As Integer, iv As Byte(), salt As Byte(), iterations As Integer, checksum As Boolean) As String
        Try
            If master Is Nothing OrElse master.Length = 0 Then Return Nothing
            Dim cipherBlocks As Integer = blockCount - 4 - If(checksum, 2, 0)
            If cipherBlocks <= 0 Then Return Nothing
            Dim key As Byte() = PKCS5_PBKDF2_HMAC(master, salt, iterations, 32)
            If key.Length = 0 Then Return Nothing
            Dim cipher(cipherBlocks * 16 - 1) As Byte
            Array.Copy(data, 64, cipher, 0, cipherBlocks * 16)
            Return Encoding.UTF8.GetString(DecryptAES(cipher, key, iv))
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ' 错密钥解出的是随机字节，会出现 U+FFFD 或控制字符。用它拒绝「解出来是乱码但没报错」的情况。
    Public Function D3LooksLikeText(plain As String) As Boolean
        If plain Is Nothing OrElse plain.Length = 0 Then Return False
        Dim hasPrintable As Boolean = False
        For Each ch As Char In plain
            If ch = ChrW(0) Then Continue For
            If AscW(ch) = &HFFFD Then Return False
            If ch < ChrW(32) AndAlso ch <> vbTab AndAlso ch <> vbCr AndAlso ch <> vbLf Then Return False
            hasPrintable = True
        Next
        Return hasPrintable
    End Function

    ''' <summary>
    ''' 解 $3 字段串。密钥来源三级：① kmc_store 命中 → ② 内置主密钥（须解得像文本）→ ③ 宿主钩子。
    ''' 返回 Nothing 表示失败；Mini 下 ③ 通常是读 --key 参数。
    ''' </summary>
    Public Function HW_D3Decrypt(param1 As String, Optional keyDir As String = "") As String
        Dim body As String = HwBytes.UnescapeXmlText(param1.Substring(2, param1.Length - 3))
        Dim n As Integer = CInt(Math.Floor(body.Length / 20))
        Dim parts As New List(Of String)
        For i As Integer = 0 To n - 1
            parts.Add(body.Substring(20 * i, 20))
        Next
        Dim raw(parts.Count * 16 - 1) As Byte
        For i As Integer = 0 To parts.Count - 1
            Array.Copy(HwBytes.HW_StrToBytes(parts(i)), 0, raw, i * 16, 16)
        Next

        If BitConverter.ToInt32(raw, 52) <> 20220323 Then Return Nothing

        Dim checksum As Boolean = raw(14) <> 0
        Dim iv(15) As Byte
        Array.Copy(raw, 16, iv, 0, 16)
        Dim salt(15) As Byte
        Array.Copy(raw, 32, salt, 0, 16)
        Dim domainId As Integer = raw(1) * 256 + raw(0)
        Dim keyId As Integer = raw(3) * 256 + raw(2)
        Dim iterations As Integer = BitConverter.ToInt32(raw, 48)

        HwKmc.LoadKeyStore(keyDir)
        Dim fromStore As Byte() = HwKmc.GetMasterKey(HwCtx.D4KeyStoreFile, domainId, keyId)
        If fromStore.Length > 0 Then
            Return D3DecryptWith(fromStore, raw, parts.Count, iv, salt, iterations, checksum)
        End If

        Dim builtin As String = D3DecryptWith(HwKeys.D3_Builtin_MasterKey, raw, parts.Count, iv, salt, iterations, checksum)
        If D3LooksLikeText(builtin) Then Return builtin

        HwCtx.D4Password = New Byte() {}
        If HwCtx.PromptPassword(keyDir) Then
            Return D3DecryptWith(HwCtx.D4Password, raw, parts.Count, iv, salt, iterations, checksum)
        End If
        Return Nothing
    End Function

    ' ---------- $4 ----------

    ''' <summary>
    ''' 解 $4 二进制块（头 64 字节 + GCM 密文）。
    ''' 密钥来源：块内 (Domain, Key) → 全 0 时退回调用方预置的 D4KeyStore（下载包头部 96/100）
    ''' → 有密钥库就按该 (Domain, Key) 查 → 查不到才用内存里的 D4Password → 最后才问宿主。
    ''' </summary>
    Public Function HW_D4Decrypt(param1 As Byte()) As Byte()
        Dim blockDomain As Integer = param1(8) * 256 + param1(9)
        Dim blockKey As Integer = param1(10) * 256 + param1(11)

        ' 下载包内层的 03 块经常把 Domain/Key 写成 0/0，真值在包头 96/100
        Dim domainId As Integer = blockDomain
        Dim keyId As Integer = blockKey
        If domainId = 0 AndAlso keyId = 0 AndAlso HwCtx.D4KeyStore.Length >= 4 Then
            domainId = HwCtx.D4KeyStore(0) * 256 + HwCtx.D4KeyStore(1)
            keyId = HwCtx.D4KeyStore(2) * 256 + HwCtx.D4KeyStore(3)
        End If

        ' 候选密钥按优先级排：人工口令 → 密钥库主密钥 → 内置默认/--key → 宿主钩子。
        ' 不能「选一个就硬用」：人工口令打错时还要能退回去试密钥库，否则一台设备都解不开。
        Dim candidates As New List(Of Byte())

        If HwCtx.PolicyPassphrase <> "" Then
            Dim byPolicy As Byte() = HwCtx.DerivePolicyPassword()
            If byPolicy.Length > 0 Then candidates.Add(byPolicy)
        End If

        If HwCtx.D4KeyStoreFile.Length > 0 Then
            Dim fromStore As Byte() = HwKmc.GetMasterKey(HwCtx.D4KeyStoreFile, domainId, keyId)
            If fromStore.Length > 0 Then candidates.Add(fromStore)
        End If

        If HwCtx.D4Password.Length > 0 Then candidates.Add(HwCtx.D4Password)

        Dim chosen As Byte() = Nothing
        Dim plain As Byte() = Nothing
        For Each cand As Byte() In candidates
            plain = TryDecryptBlock(param1, cand)
            If plain IsNot Nothing Then
                chosen = cand
                Exit For
            End If
        Next

        If chosen Is Nothing Then
            ' 前面全不对，才给宿主一次机会（GUI 是弹窗，CLI 没有钩子时直接放弃）
            If HwCtx.PromptPassword(HwCtx.D4KeyStoreLast) AndAlso HwCtx.D4Password.Length > 0 Then
                Dim manual As Byte() = HwCtx.D4Password
                plain = TryDecryptBlock(param1, manual)
                If plain IsNot Nothing Then chosen = manual
            End If
        End If

        If chosen Is Nothing Then Return New Byte() {}

        HwCtx.D4KeyStore = New Byte(3) {}
        Array.Copy(param1, 8, HwCtx.D4KeyStore, 0, 4)
        Return plain
    End Function

    ''' <summary>用给定密钥试解一个 03 块；GCM 认证失败返回 Nothing（不抛异常）。</summary>
    Private Function TryDecryptBlock(param1 As Byte(), password As Byte()) As Byte()
        If param1.Length < 65 OrElse password Is Nothing OrElse password.Length = 0 Then Return Nothing
        Try
            Dim iv(15) As Byte
            Dim salt(15) As Byte
            Array.Copy(param1, 12, iv, 0, 16)
            Dim tag(15) As Byte
            Array.Copy(param1, 28, tag, 0, 16)
            Array.Copy(param1, 44, salt, 0, 16)
            Dim cipher(param1.Length - 64 - 1) As Byte
            Array.Copy(param1, 64, cipher, 0, cipher.Length)
            Dim plain As Byte() = HwGcm.DecryptPbkdf2(password, salt, iv, cipher, tag)
            If plain Is Nothing OrElse plain.Length = 0 Then Return Nothing
            Return plain
        Catch ex As Exception
            ' GCM 认证失败是常规分支（密钥不对），返回 Nothing 由上层报错，这里只留痕
            System.Diagnostics.Debug.WriteLine("[忽略] HW_D4Decrypt: " & ex.Message)
            Return Nothing
        End Try
    End Function

    Public Function HW_D4Encrypt(param1 As Byte()) As Byte()
        Dim iv(15) As Byte
        Dim salt(15) As Byte
        Dim rnd As New Random
        rnd.NextBytes(iv)
        rnd.NextBytes(salt)
        Dim tag As Byte() = Nothing
        Dim cipher As Byte() = HwGcm.EncryptPbkdf2(HwCtx.D4Password, salt, iv, param1, tag)
        If cipher Is Nothing Then Return New Byte() {}
        Dim r(param1.Length + 64 - 1) As Byte
        r(0) = &H20
        r(1) = &H22
        r(2) = &H7
        r(3) = &H9
        Array.Copy(HwCtx.D4KeyStore, 0, r, 8, 4)
        Array.Copy(iv, 0, r, 12, 16)
        Array.Copy(tag, 0, r, 28, 16)
        Array.Copy(salt, 0, r, 44, 16)
        Dim size As Byte() = BitConverter.GetBytes(cipher.Length)
        r(60) = size(3)
        r(61) = size(2)
        r(62) = size(1)
        r(63) = size(0)
        Array.Copy(cipher, 0, r, 64, cipher.Length)
        Return r
    End Function

    Public Function HW_D4DecryptString(param1 As String) As String
        Return Encoding.UTF8.GetString(HW_D4Decrypt(Convert.FromBase64String(param1.Substring(2, param1.Length - 3))))
    End Function

    Public Function HW_D4EncryptString(param1 As String) As String
        Return "$4" & Convert.ToBase64String(HW_D4Encrypt(Encoding.UTF8.GetBytes(param1))) & "$"
    End Function

End Module
