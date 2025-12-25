using UnityEngine;
using UnityEngine.UI;

public class PropsBarPropUI : MonoBehaviour
{
    public Text PropsBarPropNameText;
    public Text PropsBarPropNumText;
    public Text PropsBarPropNeedSpaceNumText;

    public void SetUI(Prop prop)
    {
        PropsBarPropNameText.text = prop.propName;
        PropsBarPropNumText.text = prop.propCount.ToString();
        PropsBarPropNeedSpaceNumText.text = (prop.propNeedSpace * prop.propCount).ToString();
    }
}
