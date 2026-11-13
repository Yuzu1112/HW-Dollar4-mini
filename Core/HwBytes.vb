' 字节与字符串之间的转换工具。其中 HW_BytesToStr / HW_StrToBytes 是华为自有的 base93 编码，
' 不是标准 Base64 —— 每 4 字节映射成 5 个可见 ASCII 字符，值域 33..126（126 代表 30）。
Imports System
Imports System.Linq
Imports System.Text

Public Module HwBytes

    Public Function BytesToHex(param1 As Byte()) As String
        Return BitConverter.ToString(param1).Replace("-", "").ToUpper
    End Function

    Public Function HexToBytes(param1 As String) As Byte()
        Return Enumerable.Range(0, param1.Length).Where(Function(x) x Mod 2 = 0).[Select](Function(x) Convert.ToByte(param1.Substring(x, 2), 16)).ToArray()
    End Function

    Public Function BytesToBin(param1() As Byte) As String
        Dim sb As New StringBuilder
        For Each b In param1
            sb.Append(Convert.ToString(b, 2).PadLeft(8, "0"c))
        Next
        Return sb.ToString()
    End Function

    Public Function BinToBytes(param1 As String) As Byte()
        Dim n As Integer = param1.Length \ 8
        Dim r(n - 1) As Byte
        For i As Integer = 0 To n - 1
            r(i) = Convert.ToByte(param1.Substring(8 * i, 8), 2)
        Next
        Return r
    End Function

    ''' <summary>读 4 字节小端无符号整数。与原实现一致，不做边界检查。</summary>
    Public Function BytesToInt32(param1 As Byte()) As Integer
        Return Int(param1(0)) + Int(param1(1)) * 256 + Int(param1(2)) * 65536 + Int(param1(3)) * 16777216
    End Function

    Public Function GetBigEndian32(param1 As Byte(), param2 As Integer) As UInteger
        Dim b(3) As Byte
        Array.Copy(param1, param2, b, 0, 4)
        If BitConverter.IsLittleEndian Then Array.Reverse(b)
        Return BitConverter.ToUInt32(b, 0)
    End Function

    Public Function GetLittleEndian32(param1 As Byte(), param2 As Integer) As UInteger
        Dim b(3) As Byte
        Array.Copy(param1, param2, b, 0, 4)
        If Not BitConverter.IsLittleEndian Then Array.Reverse(b)
        Return BitConverter.ToUInt32(b, 0)
    End Function

    ''' <summary>16 字节 → 20 个可见 ASCII 字符（base93 编码）。输入必须是 16 字节。</summary>
    Public Function HW_BytesToStr(param1 As Byte()) As String
        Dim out(19) As Byte
        For blk As Integer = 0 To 3
            Dim v As Long = BitConverter.ToUInt32(New Byte() {param1(blk * 4), param1(blk * 4 + 1), param1(blk * 4 + 2), param1(blk * 4 + 3)}, 0)
            Dim div As Long = 74805201
            For i As Integer = 4 To 0 Step -1
                out(blk * 5 + i) = CByte(Math.Floor(v / div))
                v = v Mod div
                div \= 93
            Next
        Next
        For i As Integer = 0 To 19
            If out(i) = 30 Then
                out(i) = 126
            Else
                out(i) = CByte(out(i) + 33)
            End If
        Next
        Return Encoding.UTF8.GetString(out)
    End Function

    ''' <summary>20 个可见 ASCII 字符 → 16 字节（base93 解码）。输入必须正好 20 字符。</summary>
    Public Function HW_StrToBytes(param1 As String) As Byte()
        Dim s As Byte() = Encoding.UTF8.GetBytes(param1)
        For i As Integer = 0 To 19
            If s(i) = 126 Then
                s(i) = 30
            Else
                s(i) = CByte(s(i) - 33)
            End If
        Next
        Dim out(15) As Byte
        For blk As Integer = 0 To 3
            Dim v As Long = 0
            Dim mul As Long = 1
            For i As Integer = 0 To 4
                v += mul * s(blk * 5 + i)
                mul *= 93
            Next
            Dim t As Byte() = BitConverter.GetBytes(v)
            out(blk * 4 + 0) = t(0)
            out(blk * 4 + 1) = t(1)
            out(blk * 4 + 2) = t(2)
            out(blk * 4 + 3) = t(3)
        Next
        Return out
    End Function

    ''' <summary>字段串里的 XML 转义还原。$1..$4 密文可能被写进 XML 属性，读回时要先反转义。</summary>
    Public Function UnescapeXmlText(s As String) As String
        Return s.Replace("&quot;", """").Replace("&amp;", "&").Replace("&apos;", "'").Replace("&lt;", "<").Replace("&gt;", ">")
    End Function

    ''' <summary>把字段串转义成可安全放进 XML 的形状。</summary>
    Public Function EscapeXmlText(s As String) As String
        Return s.Replace("&", "&amp;").Replace("""", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;")
    End Function

End Module
