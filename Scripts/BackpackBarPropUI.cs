using UnityEngine;
using UnityEngine.UI;

public class BackpackBarPropUI : MonoBehaviour
{
    public Text backpackBarPropNameText;
    public Text backpackBarPropNumText;
    public Text backpackBarPropNeedSpaceNumText;
    public Button backpackBarClickButton;
    public float backpackBarPropEachNeedSpaceNum;//用来保留该道具的单个占用空间


    public void SetUI(Prop prop)
    {
        backpackBarPropEachNeedSpaceNum = prop.propNeedSpace;
        backpackBarPropNameText.text = prop.propName;
        backpackBarPropNumText.text = prop.propCount.ToString();
        backpackBarPropNeedSpaceNumText.text = (prop.propNeedSpace * prop.propCount).ToString();
        backpackBarClickButton.onClick.AddListener(() => { BackpackBarUIButtonDown(); });
    }
    public void BackpackBarUIButtonDown()
    {
        GameController.Instance.ReducePropOutside(backpackBarPropNameText.text, 1);
        GameController.Instance.AddPropInHome(backpackBarPropNameText.text, backpackBarPropEachNeedSpaceNum, 1);
        GameController.Instance.ChangeCanRefreshPropsToTrue();
    }
}
