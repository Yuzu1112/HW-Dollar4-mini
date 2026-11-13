' GZip 封装。注意华为用的是标准 GZipStream 输出，其尾部 4 字节是 ISIZE（原始长度，小端），
' 所以 UnGZip 默认从末尾取长度是合法的 —— 不是自定义尾标。
Imports System
Imports System.IO
Imports System.IO.Compression

Public Module HwGzip

    ''' <summary>压缩。输出为标准 GZip 流（含 CRC32 + ISIZE 尾部）。</summary>
    Public Function GZip(param1 As Byte()) As Byte()
        Dim ms As New MemoryStream()
        Using gz As New GZipStream(ms, CompressionMode.Compress, True)
            gz.Write(param1, 0, param1.Length)
        End Using
        ms.Position = 0
        Dim out(CInt(ms.Length - 1)) As Byte
        ms.Read(out, 0, out.Length)
        Return out
    End Function

    ''' <summary>
    ''' 解压。param2 = True（默认）表示 4 字节长度在末尾（标准 GZip 的 ISIZE 尾）；
    ''' param2 = False 表示 4 字节长度在开头（华为 CTCE8 容器用这种前置长度格式）。
    ''' </summary>
    Public Function UnGZip(param1 As Byte(), Optional param2 As Boolean = True) As Byte()
        Using ms As New MemoryStream()
            Dim size As Integer
            If param2 Then
                size = BitConverter.ToInt32(param1, param1.Length - 4)
                ms.Write(param1, 0, param1.Length - 4)
            Else
                size = BitConverter.ToInt32(param1, 0)
                ms.Write(param1, 4, param1.Length - 4)
            End If
            Dim out(size - 1) As Byte
            ms.Position = 0
            Using gz As New GZipStream(ms, CompressionMode.Decompress)
                gz.Read(out, 0, out.Length)
            End Using
            Return out
        End Using
    End Function

End Module
