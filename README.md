# HW Dollar4 mini

华为 ONT 配置文件格式转换工具 · 精简版 **4.44.0.2611**

> 算法实现来自 [ipatrina/HW-Dollar4](https://github.com/ipatrina/HW-Dollar4)，遵循 GPL v2。
> 本仓库是独立精简版，加解密能力与 [HW Dollar4](https://github.com/Yuzu1112/HW-Dollar4) 主程序对齐。

## 它和主程序的区别

| | HW Dollar4 | HW Dollar4 mini |
|---|---|---|
| 定位 | 打开配置、编辑、另存 | 只做格式转换，不做编辑 |
| 界面 | 完整主窗体 + 查找替换 | 单窗口，文件丢进去换种形态吐出来 |
| 命令行 | 无 | 有，支持批处理 |
| 落盘 | 手动保存 | 原文件改名为 `001_原名`，结果写回原名 |
| 加解密能力 | 见下 | **与主程序一致，另多两项** |

多出来的两项：`20 22 07 09` $4 裸块可直接产出；支持 GZip 裸流（`1F 8B`）解压。

## 支持的容器与字段

| 格式 | 解密 | 加密 |
|---|:--:|:--:|
| `01` $2 静态容器（gzip） | ✓ | ✓ |
| `01` $2 静态容器（不压缩，Other） | ✓ | ✓ |
| `02` $2 动态容器 | ✓ | ✓ |
| `03` $4 容器 | ✓ | ✓ |
| `20 22 07 09` $4 裸块 | ✓ | ✓ |
| `07 12 21 20` 下载包（内层 01/02/03） | ✓ | ✓ |
| `boardinfo` V5 / V6（20 字节头） | ✓ | ✓ |
| `67 66 63 71` CFG（ctce8） | ✓ | ✗ |
| GZip 裸流 | ✓ | ✗ |
| KSF v2（kmc_store）密钥库 | ✓ 查密钥 / 列 Master Key | — |
| 字段 `$1` / `$2` / `$3` / `$4` | ✓ | `$1`/`$2`/`$4` |

## 命令行

```
HwD4mini.exe <文件...>                  自动：密文解密、明文加密
HwD4mini.exe dec <文件...>              强制解密
HwD4mini.exe enc <文件...> [--to X]     强制加密
HwD4mini.exe info <文件...>             只识别格式
HwD4mini.exe field <字符串>             字段串解密；非 $ 开头则加密
HwD4mini.exe kmc [密钥库]               列出 Master Key

--to s1|s2|s4        01 / 02 / 03，默认 s2
     pkg|pkg1|pkg4   下载包（内层 02 / 01 / 03）
     v5|v6|bin4      boardinfo V5 / V6 / $4 裸块
     text|raw1       导出明文 / 01 容器但不压缩
--key <HEX>          指定 $3/$4 主密钥（32 字节 HEX）
--pass <口令>        人工口令：解密内层 $4，或加密 pkg4 时派生密钥
--kmc <路径>         指定密钥库文件或目录
--d2key os|boardinfo|omci   $2 容器密钥盐
-y                   不备份，直接覆盖
-f                   允许对密文二次加密（默认拒绝）
```

双击不带参数则打开图形界面。

## 系统要求

- Windows 10 及以上版本 64 位操作系统
- Microsoft .NET Framework 4.7.2 及以上版本

## 构建

Visual Studio 2019 及以上打开 `HW Dollar4 mini.sln`，Release 构建即可。

加密实现（`Crypto/`、`Core/`）已随仓库自带，**不依赖任何外部仓库或原生 DLL**。工程开启了 `Option Strict On`。

## 开源与致谢

本项目基于 **ipatrina** 的 [HW Dollar4](https://github.com/ipatrina/HW-Dollar4) 二次开发，算法研究、容器格式与密钥派生逻辑均来自该项目，在此致谢。

HW Dollar4 mini 使用以下开源组件，其源代码可通过对应的项目页面获取：

- aescrypt2 (1.0): https://packetstorm.news/files/id/35655
- Mbed TLS (3.6.0 LTS): https://github.com/Mbed-TLS/mbedtls

本项目遵循 **GNU General Public License v2**，见 [LICENSE](LICENSE)。
