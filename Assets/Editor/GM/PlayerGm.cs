using Sirenix.OdinInspector;

public class PlayerGm : GmEditor.Menu
{
    public override string MenuName => "玩家信息";
    
    [BoxGroup("金币")]
    [LabelText("数量")]
    public int gold;

    [BoxGroup("金币")]
    [Button("添加")]
    private void B()
    {
        
    }
}