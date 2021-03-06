# Mikibot

这个项目本意是想建立一个直播数据分析工具，比如某天播了什么就引了个大流什么的，或者是探究（B站）直播间发红包引流效果之类的问题。

本体目前在群 1001856303 试运营！

## 已有功能
| 功能名称 | 简介 | 实现位置 |
| - | - | - |
| 特定弹幕开始自动切片 | 发送！！结尾的弹幕开始切片，发送！！！结尾的弹幕停止切片。 | [DanmakuRecordControlService](Mikibot.Analyze/Notification/DanmakuRecordControlService.cs) [ClipperController](Mikibot.AutoClipper/Service/ClipperController.cs) |
| 粉丝数量统计 | 记录涨粉趋势，每15秒统计粉丝数量 | [DailyFollowerStatisticService](Mikibot.Analyze/Notification/DailyFollowerStatisticService.cs) |
| 全量弹幕装填 | 全量记录直播间弹幕，发送人、发送时间、发送时粉丝牌等等 | [DanmakuCollectorService](Mikibot.Analyze/Notification/DanmakuCollectorService.cs) |
| 舰长开通/进房统计 | 全量记录上舰和舰长进房（关闭进入提醒无法记录） | [DanmakuCollectorService](Mikibot.Analyze/Notification/DanmakuCollectorService.cs) |
| 直播间进入统计 | 普通用户进入房间记录 | [DanmakuCollectorService](Mikibot.Analyze/Notification/DanmakuCollectorService.cs) |
| 直播间SC统计 | 全量SC记录，发送人、金额和内容 | [DanmakuCollectorService](Mikibot.Analyze/Notification/DanmakuCollectorService.cs) |
| 直播间送礼统计 | 免费、付费礼物记录 | [DanmakuCollectorService](Mikibot.Analyze/Notification/DanmakuCollectorService.cs) |
| 上下播通知、单次直播粉丝数量统计 | 可以通知到QQ群 | [LiveStatusCrawlService](Mikibot.Analyze/Notification/LiveStatusCrawlService.cs) |
| 发送弹幕支持导出舰长列表到邮箱 | 发送“#导出舰长列表”可向特定邮箱发送当前舰长信息。 | [DanmakuExportGuardList](Mikibot.Analyze/Notification/DanmakuExportGuardList.cs) |

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


