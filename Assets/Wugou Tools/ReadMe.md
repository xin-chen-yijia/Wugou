# SiQi devlopment tools for Unity3D
封装Unity3D中常用一些功能，避免重复开发，提升开发效率。

BuZheng Dev tools  
   |  
   |  ____Assetbundle Assetbundle资源创建和加载;  
   |  
   |  ____Scripts
   |          |
   |          |____Player  角色控制、摄像机控制脚本，如第一人称、第三人称视角等；  
   |          |
   |          |____Common 常用的一些功能代码;  
   |          |
   |          |____Core 常用的一些功能代码，如协程、对象池;  
   |          |
   |          |____UI 配合Assetbundle，把UI资源放到AB中，动态加载;  
   |          |
   |          |____XR XR开发中的一些总结，如手动开启SteamVR；因为依赖OpenXR的包，用`SIQI_VR`宏来控制是否启用；


# 问题
1. 提示newtonsoft Json找不到；
解决方案：在“package manager”中安装"Newtonsoft Json"包即可；

2. 坐标轴无法选中
解决方案： 查看"Axis"的layer，必须在“MapTool”下才可以选中；