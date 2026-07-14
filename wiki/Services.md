# 服务

> SDK `0.1.0-alpha.3`

首版公共服务 ID：

- `pcl.logging`
- `pcl.dispatcher`
- `pcl.notifications`
- `pcl.settings`
- `pcl.commands`
- `pcl.tasks`
- `pcl.instances.read`
- `pcl.ui`
- `pcl.ui.patch`
- `pcl.market`

使用 `context.Services.Require<T>()` 获取必需服务。服务必须在 Manifest 的 `services.required` 或 `services.optional` 中声明版本范围。实例服务只返回公开 DTO，不返回 PCL-N 内部对象。
