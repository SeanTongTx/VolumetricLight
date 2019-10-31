# VolumetricLight/Version

## UPdate
### 090517
ColorAlpha现在可以正常工作了
修复了Quad材质刷新问题
修改了dust粒子清除。在unity2019prefab中不能直接删除已经序列化在Prefab中的粒子发射器对象。
### 090514
LOD level1 将所有的计算移入到顶点计算中。减少像素计算
恢复了level1 对内外Alpha的支持
### 0190509
为了解决像素运算过高的问题,增加了LOD模式。
目前只有Level 0 使用原始运算，Level 1使用简化后算法。
移除了一些用不到的效果。