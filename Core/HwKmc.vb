' KSFv2 密钥库（kmc_store / kmc_store_A / kmc_store_B）解析。
' 结构：34 字节魔数 → 迭代次数(52) → 记录数(184) → 每 256 字节一条记录。
' 每条记录的根密钥 = PBKDF2(seed xor mask xor salt, salt, iterations, 32)。
Imports System
Imports System.Collections.Generic
Imports System.IO

''' <summary>密钥库中的一条记录。</summary>
Public Class KmcEntry
    Public Property DomainId As Integer
    Public Property KeyId As Integer
    Public Property MasterKey As Byte()
    Public ReadOnly Property MasterKeyHex As String
        Get
            Return HwBytes.BytesToHex(MasterKey)
        End Get
    End Property
End Class

Public Module HwKmc

    ''' <summary>判断字节流是否为 KSFv2 密钥库。</summary>
    Public Function IsKeyStore(buf As Byte()) As Boolean
        If buf Is Nothing OrElse buf.Length < 34 Then Return False
        For i As Integer = 0 To 33
            If buf(i) <> HwKeys.KSFv2_Magic(i) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' 定位并加载密钥库。查找顺序：指定目录 → 上次目录 → 程序目录，各自先 _A 后 _B。
    ''' 找不到时把 D4KeyStoreFile 置空（不是抛错）。
    ''' </summary>
    Public Sub LoadKeyStore(Optional dir As String = "")
        Dim baseDir As String = If(String.IsNullOrEmpty(dir), HwCtx.D4KeyStoreLast, dir)
        If String.IsNullOrEmpty(baseDir) Then baseDir = AppDir()
        If Directory.Exists(baseDir) Then HwCtx.D4KeyStoreLast = baseDir

        Dim candidates As String() = {
            Path.Combine(baseDir, "kmc_store_A"),
            Path.Combine(baseDir, "kmc_store_B"),
            Path.Combine(AppDir(), "kmc_store_A"),
            Path.Combine(AppDir(), "kmc_store_B"),
            Path.Combine(AppDir(), "kmc_store")
        }
        For Each c As String In candidates
            If File.Exists(c) Then
                HwCtx.D4KeyStoreFile = File.ReadAllBytes(c)
                Return
            End If
        Next
        HwCtx.D4KeyStoreFile = New Byte() {}
    End Sub

    Public Function AppDir() As String
        Return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)
    End Function

    ' 解出根密钥：seed(56..87) xor salt(88..119) xor mask。
    Private Function DeriveRootKey(ksf As Byte()) As Byte()
        Dim seed(31) As Byte
        For i As Integer = 0 To 31
            seed(i) = ksf(56 + i) Xor ksf(88 + i) Xor HwKeys.KSFv2_Mask(i)
        Next
        Dim salt(31) As Byte
        Array.Copy(ksf, 152, salt, 0, 32)
        Dim iterations As Integer = CInt(HwBytes.GetBigEndian32(ksf, 52))
        Return HwField.PKCS5_PBKDF2_HMAC(seed, salt, iterations, 32)
    End Function

    ''' <summary>按 (DomainID, KeyID) 取主密钥。取不到返回空数组。</summary>
    Public Function GetMasterKey(ksf As Byte(), domainId As Integer, keyId As Integer) As Byte()
        Try
            If Not IsKeyStore(ksf) Then Return New Byte() {}
            Dim recordCount As Integer = CInt(HwBytes.GetBigEndian32(ksf, 184))
            Dim rootKey As Byte() = DeriveRootKey(ksf)
            For r As Integer = 0 To recordCount - 1
                Dim off As Integer = (r + 1) * 256
                If off + 256 > ksf.Length Then Exit For
                If CInt(HwBytes.GetBigEndian32(ksf, off + 0)) = domainId AndAlso CInt(HwBytes.GetBigEndian32(ksf, off + 4)) = keyId Then
                    Return ReadRecord(ksf, off, rootKey)
                End If
            Next
            Return New Byte() {}
        Catch ex As Exception
            Return New Byte() {}
        End Try
    End Function

    Private Function ReadRecord(ksf As Byte(), off As Integer, rootKey As Byte()) As Byte()
        Dim encLen As Integer = CInt(HwBytes.GetBigEndian32(ksf, off + 88))
        Dim keyLen As Integer = CInt(HwBytes.GetBigEndian32(ksf, off + 92))
        Dim iv(15) As Byte
        Array.Copy(ksf, off + 72, iv, 0, 16)
        Dim enc(encLen - 1) As Byte
        Array.Copy(ksf, off + 96, enc, 0, encLen)
        Dim plain As Byte() = HwField.DecryptAES(enc, rootKey, iv)
        Dim key(keyLen - 1) As Byte
        Array.Copy(plain, 0, key, 0, keyLen)
        Return key
    End Function

    ''' <summary>列出全部记录。GUI 版拿它渲染表格，CLI 版直接打印。</summary>
    Public Function GetAllMasterKeys(ksf As Byte()) As List(Of KmcEntry)
        Dim list As New List(Of KmcEntry)
        Try
            If Not IsKeyStore(ksf) Then Return list
            Dim recordCount As Integer = CInt(HwBytes.GetBigEndian32(ksf, 184))
            Dim rootKey As Byte() = DeriveRootKey(ksf)
            For r As Integer = 0 To recordCount - 1
                Dim off As Integer = (r + 1) * 256
                If off + 256 > ksf.Length Then Exit For
                Dim key As Byte() = ReadRecord(ksf, off, rootKey)
                list.Add(New KmcEntry With {
                    .DomainId = CInt(HwBytes.GetBigEndian32(ksf, off + 0)),
                    .KeyId = CInt(HwBytes.GetBigEndian32(ksf, off + 4)),
                    .MasterKey = key
                })
            Next
        Catch ex As Exception
        End Try
        Return list
    End Function

End Module
