// IEntityRuntimeDependent.cs
// 放在你的 Core/EntityData 資料夾下
public interface IEntityRuntimeDependent
{
    // 當大腦資料初始化，或是被外部強制替換時，會呼叫此方法
    void OnRuntimeDataChanged(EntityRuntime newData);
}