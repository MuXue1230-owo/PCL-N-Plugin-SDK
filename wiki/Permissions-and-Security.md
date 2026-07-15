# 权限与安全

> SDK `0.1.0`，运行时行为以 PCL.Plugin `v0.12.0` 为准。

插件在 PCL N 进程内运行。权限声明用于描述和约束官方 API、支持审核和用户决策，但不是操作系统级沙箱。用户仍应只安装可信发布者的有效签名包。

## 当前已知权限 ID

### 稳定服务

- `pcl.logging`
- `pcl.dispatcher`
- `pcl.notifications`
- `pcl.settings`
- `pcl.settings-pages`
- `pcl.commands`
- `pcl.tasks`
- `pcl.instances.read`
- `pcl.ui`
- `pcl.ui.surface`
- `pcl.ui.patch`
- `pcl.network.limited`

### UI

- `ui.register`
- `ui.observe`
- `ui.inject`
- `ui.modify`
- `ui.replace`
- `ui.resources`
- `ui.global`
- `ui.raw-access`
- `ui.input-intercept`
- `ui.window-management`

未知权限会 fail closed：运行时拒绝加载，而不是静默忽略拼写错误。

## 写好 reason

```json
"permissions": [
  {
    "id": "pcl.instances.read",
    "reason": "读取实例名称和目录，为用户生成本地备份报告。"
  },
  {
    "id": "ui.inject",
    "reason": "在启动页主按钮下方显示备份状态。"
  }
]
```

reason 应说明“读/写什么、为什么、用户得到什么”，不要写“插件需要”或“正常运行”。

## 高风险能力

以下能力需要特别谨慎并接受更严格审核：

- 全局 UI、Raw UI 与窗口管理；
- 输入拦截；
- replace、modify、资源/样式/模板覆写；
- 账户和启动流程；
- 原生代码；
- 网络和外部进程。

能用稳定服务或 `inject` 完成时，不要申请 `raw-access` 或 `replace`。

## AXAML 安全规则

- 禁止 `x:Class` 和代码隐藏；
- 禁止 PCL N 私有 CLR 命名空间；
- 只加载 Manifest 预先声明的包内安全路径；
- 事件通过公开命令绑定；
- Host 限制可实例化类型、MarkupExtension 和资源 URI；
- UI Safe Mode 可跳过高风险 Patch。

## 文件系统

使用 `context.Directories` 访问当前插件的 Root/Data/Cache/Logs/Temp。不要扫描启动器安装目录、其他插件目录或用户无关文件。

简单配置使用 `IPluginSettingsStore`，避免自行实现不安全的路径拼接。

## 网络与隐私

- 只请求完成明确功能所需的最小数据；
- 在 Manifest、市场说明和 UI 中解释网络用途；
- 不记录 Token、Cookie、订单号、私钥或完整个人资料；
- 为网络调用设置超时并响应取消；
- 不把服务端 Secret 放入插件程序集；
- 账户相关数据优先走宿主公开服务，而不是绕过宿主自行抓取。

## 签名不是万能的

OpenPGP 签名证明包内容与某个密钥一致，不证明代码无漏洞或发布者永远可信。插件中心审核、文件哈希、权限声明、运行时边界和用户判断需要共同工作。
