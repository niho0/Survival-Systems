using UnityEngine;
using UnityEngine.UI;

public class PropsDiscardBarPropUI : MonoBehaviour
{
    public  Text propsDiscardPropNameText;
    public  Text propsDiscardPropNumText;
    public  Text propsDiscardPropNeedSpaceNumText;
    public  InputField propsDiscardPropDiscardInputField;
    public float propsDiscardPropEachNeedSpaceNum;
    
    public void SetUI(Prop prop)
    {
        propsDiscardPropEachNeedSpaceNum = prop.propNeedSpace;
        propsDiscardPropNameText.text = prop.propName;
        propsDiscardPropNumText.text = prop.propCount.ToString();
        propsDiscardPropNeedSpaceNumText.text = (prop.propNeedSpace * prop.propCount).ToString();//该物品总需要空间
    }
}
