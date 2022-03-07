# Mikibot

这个项目本意是想建立一个直播数据分析工具，比如某天播了什么就引了个大流什么的，或者是探究（B站）直播间发红包引流效果之类的问题。

本体目前在群 1001856303 试运营！

## 已有功能
- 粉丝数量统计
- 全量弹幕装填
- 舰长开通/进房统计
- 直播间进入统计
- 直播间SC统计
- 直播间送礼统计
- 上下播通知、单次直播粉丝数量统计
- 特定弹幕开始自动切片
- 发送弹幕支持导出舰长列表到邮箱

## 技术栈
- 网站后端 **Backend** ASP.NET Core + C#
- 网站前端 **Frontend** React 17 + Typescript
- 数据处理 **Analyze** C#
- **Crawler** C#

## 项目结构
- **Mikibot.API** - Mikibot网站的后端
- **Mikibot.FE** - Mikibot网站的前端
- **Mikibot.Analyze** - Mikibot本体
- **Mikibot.AutoClipper** - 切片服务
- **Mikibot.Database** - 数据库访问和相关模型
- **Mikibot.Crawler** - Crawler相关，包括HTTP API和Websocket相关
- **Mikibot.BuildingBlocks** - 通用组件


