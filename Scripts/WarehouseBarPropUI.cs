using UnityEngine;
using UnityEngine.UI;

public class WarehouseBarPropUI : MonoBehaviour
{
    public Text warehouseBarPropNameText;
    public Text warehouseBarPropNumText;
    public Text warehouseBarPropNeedSpaceNumText;
    public Button warehouseBarClickButton;
    public float warehouseBarPropEachNeedSpaceNum;//用来保留该道具的单个占用空间

    public void SetUI(Prop prop)
    {
        warehouseBarPropEachNeedSpaceNum = prop.propNeedSpace;
        warehouseBarPropNameText.text = prop.propName;
        warehouseBarPropNumText.text = prop.propCount.ToString();
        warehouseBarPropNeedSpaceNumText.text = (prop.propNeedSpace * prop.propCount).ToString();
        warehouseBarClickButton.onClick.AddListener(() => {WarehouseBarUIButtonDown(); });
    }
    public void WarehouseBarUIButtonDown()
    {
        GameController.Instance.ReducePropInHome(warehouseBarPropNameText.text,1);
        GameController.Instance.AddPropOutside(warehouseBarPropNameText.text,warehouseBarPropEachNeedSpaceNum,1);
        GameController.Instance.ChangeCanRefreshPropsToTrue();
    }
}
