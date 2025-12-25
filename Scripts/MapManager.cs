
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [SerializeField] private GameObject targetPre;//目标点的预制体

    [SerializeField] private GameObject mapCanvas;//mapUI的画布

    [SerializeField] private GameObject playerNowPointPre;//用于承载玩家已到达的位置的标识

    [SerializeField] private GameObject CityButtonPre;//用于承载目标城市的预制体

    [SerializeField] private GameObject carryFoodsBar;//携带食物栏

    [SerializeField] private GameObject tipBar;//提示栏,在用户要做出非法操作时蹦出来提示

    [SerializeField] private GameObject backgroundBlocker;//背景遮盖，需要时打开使背景虚化

    [SerializeField] private GameObject background;//背景板

    [SerializeField] private GameObject managePropsBar;//管理道具栏

    [SerializeField] private GameObject warehouseBarPropsUIPre;//仓库栏生成道具的ui

    [SerializeField] private GameObject backpackBarPropsUIPre;//背包栏生成道具的ui

    [SerializeField] private Button homeButton;//用于承载Home

    [SerializeField] private Transform[] spawnTrans;//用于承载City刷新位置

    [SerializeField] private InputField carryFoodsInputField;//携带多少食物的输入栏

    [SerializeField] private RawImage fogImage;//迷雾画板

    [SerializeField] private Text ownFoodsNumText;//拥有多少食物的text

    [SerializeField] private Text needFoodsNumText;//预期需要多少食物的Text

    [SerializeField] private Text remainingSpaceNumText;//管理道具栏中剩余空间数量文本

    [SerializeField] private Text portableSpaceNumText;//所拥有空间文本

    [SerializeField] private Texture2D fogTexture;//战争迷雾纹理

    [SerializeField] private Transform warehouseBarPropsContainer;//容纳仓库道具陈列ui的Grid

    [SerializeField] private Transform backpackBarPropsContainer;//容纳仓库道具陈列ui的Grid

    [SerializeField] private float playerRevealRadius = 0.2f;//玩家调查揭露的迷雾的半径大小

    [SerializeField] private float homeRevealRadius = 0.4f;//房子揭露的迷雾的半径大小


    private Vector2 mousePosition;//用于记录鼠标点击位置，用于在点击事件中给controller传输目标位置

    private bool isCityButtonDown = false;//用来判断是否是City按钮按下呼出的CarryFoodsBar，用于辅助判定是departure还是进入city路线的departure

    private GameObject playerNowPointTemp;//用来临时承载player所在位置

    private GameObject cityButtonTemp;//用来临时承载生成的cityButton

    private GameObject targetPreTemp;//用于承载目标点的临时游戏物体



    void Start()
    {
        CheckSelectFlag();
        UpdateFog(homeButton.transform.position,homeRevealRadius);
        SetFogImage();
        SetupFogClickHandling();
        SetupBackgroundClickHandling();
        if (GameController.Instance != null)
        {
            if (GameController.Instance.CitySpawnPointNum == 0)//检测是否已经取了随机数，如果已经生成了随机数就不用再产生一个随机数，直接用那个随机数就行
                                                               //用于设置最终点City
            {
                SetCityPositionNum();
                CreateCityPosition();
            }
            else
            {
                CreateCityPosition();
            }
            SetPlayerNowPoint();
            SetFirstGameResource();
            AfterFogUpdate();//这是必要的，因为在揭露home时city还没生成
        }
        GeneratePropsInWarehouseBar();
        GeneratePropsInBackpackBar();
        RadioEvent();
        RecoverAfterReturningHome();
    }

    void Update()
    {
        if (GameController.Instance != null)
        {
            SetTargetPoint();
            ContinuePrintManagePropsBarText();
            RefreshProps();
        }
    }

    public void SetTargetPoint()//进行目标点的设置
    {
        if (Input.GetMouseButtonDown(0))//用户点击左键
        {
            if (!EventSystem.current.IsPointerOverGameObject())//如果点击的不是UI
            {
                if (targetPreTemp != null)//如果临时目标点赋过值
                {
                    Destroy(targetPreTemp);//销毁原目标点
                }
                mousePosition = Input.mousePosition;//记录鼠标位置
                targetPreTemp = Instantiate(targetPre, new Vector3(mousePosition.x, mousePosition.y, 0), Quaternion.identity, mapCanvas.transform);//为鼠标位置的地方生成目标点预制体
                targetPreTemp.transform.SetSiblingIndex(mapCanvas.transform.childCount-4);//需要在弹窗下面，在迷雾上面
                Button button = targetPreTemp.GetComponent<Button>();//从TargetButton预制体得到Button组件
                button.onClick.AddListener(() => { TargetButtonDown(); });//添加跳转场景事件
            }
        }
    }

    public void TargetButtonDown()//目标点Button点击事件
    {
        if (GameController.Instance.IsInHome)//如果在家里才显示携带多少食物的输入栏
        {
            SetActions(new Vector3(mousePosition.x, mousePosition.y, 0));//如果步数不满1直接弹窗提示
            if (GameController.Instance.Actions <= 0)
            {
                GiveATip("我奶奶都能走过去");
                return;
            }
            OpenCarryFoodsBarAndSetNum(GetFoodsConsumption(new Vector3(mousePosition.x, mousePosition.y, 0)));
        }
        else
        {
            Departure();
        }

    }

    public void SetPlayerNowPoint()//设置上一阶段所到达位置
    {
        if (!GameController.Instance.IsInHome)//判断是否在家，即是否是第一次出行，即是否上一次没有设置目标位置
        {
            playerNowPointTemp = Instantiate(playerNowPointPre,
                new Vector3(GameController.Instance.PlayerNowPosition.x, GameController.Instance.PlayerNowPosition.y, 0), Quaternion.identity, mapCanvas.transform);
            //在到达之前目标位置后在之前的目标位置建立所在位置图标
            playerNowPointTemp.transform.SetSiblingIndex(1);
            UpdateFog(GameController.Instance.PlayerNowPosition, playerRevealRadius);
        }
    }

    public void SetActions(Vector3 targetPosition)//设置行走的步数，目前仅跟目标点和起始点的直线距离有关
    {
        float distance;//用于承载距离，用于计算步数
        if (GameController.Instance.IsInHome)//判断是否在家，即是否从出生点开始计算距离
        {
            distance = Vector3.Distance(homeButton.transform.position, new Vector3(mousePosition.x, mousePosition.y, 0));
        }
        else
        {
            distance = Vector3.Distance(GameController.Instance.PlayerNowPosition, targetPosition);
        }
        int actions = (int)distance / 10;//通过距离计算步数
        GameController.Instance.SetActions(actions);//把步数通过控制器的设置步数方法来设置
    }

    public float GetFoodsConsumption(Vector3 targetPosition)//用来计算这段路程所需要消耗的食物,只用于从家出发所需食物的文本显示
    {//绝对是从家里出来的，所以IsInHome一定是true
        float distance;//用于承载距离，用于计算步数
        distance = Vector3.Distance(homeButton.transform.position, targetPosition);
        float foodsConsumption = (int)((distance / 10) * 10f) / 10f;
        return foodsConsumption;
    }

    public void SetCityPositionNum()//设置city出生点位的随机数
    {
        int randomInt = UnityEngine.Random.Range(1, 5);//随机抽取数据1-4,0号位置随便放一个就行
        GameController.Instance.SetCitySpawnPointNum(randomInt);//把随机数给游戏控制器
        switch (randomInt)
        {
            case 1:
                GameController.Instance.SetCityDirection("西北");
                break;
            case 2:
                GameController.Instance.SetCityDirection("西南");
                break;
            case 3:
                GameController.Instance.SetCityDirection("东北");
                break;
            case 4:
                GameController.Instance.SetCityDirection("东南");
                break;
        }
    }

    public void CreateCityPosition()//通过生成的城市出生点位的随机数来生成城市
    {
        cityButtonTemp = Instantiate(CityButtonPre, spawnTrans[GameController.Instance.CitySpawnPointNum].position,
            Quaternion.identity, mapCanvas.transform);//生成City,从游戏控制器拿数据
        cityButtonTemp.transform.SetSiblingIndex(1);//将按钮生成在画布最下面，防止在遮罩上面
        Button button = cityButtonTemp.GetComponent<Button>();//从CityButtonPre预制体得到Button组件
        button.onClick.AddListener(() => { CityButtonDown(); });//添加City按钮事件
    }

    public void CityButtonDown()//City按钮点击事件
    {
        if (GameController.Instance.IsInHome)//如果在家里才显示携带多少食物的输入栏
        {
            OpenCarryFoodsBarAndSetNum(GetFoodsConsumption(spawnTrans[GameController.Instance.CitySpawnPointNum].position));
            isCityButtonDown = true;
        }
        else
        {//city路线的不在家Departure
            SetActions(spawnTrans[GameController.Instance.CitySpawnPointNum].position);
            GameController.Instance.SetOutHome();//只要点击了这个按钮必然会离家
            GameController.Instance.SetLastStep();//设置最后一步
            if (GameController.Instance.Actions <= 0)
            {
                SceneManager.LoadScene(4);//因为最后需要进行的步数为零,不需要任何代价就能结束游戏，所以直接进入总结场景
                return;
            }
            SceneManager.LoadScene(3);//带着最后一步进行PLAY场景
        }
    }

    public void SetFirstGameResource()//用来设置第一次游戏的资源
    {
        if (GameController.Instance.IsFirstGame)//如果游戏控制器中显示是第一次游戏
        {
            GameController.Instance.GetFirstGameResource();
        }
    }

    public void HomeButttonDown()//Home按钮的点击事件
    {
        if (!GameController.Instance.IsInHome)//不在家时
        {
            SetActions(homeButton.transform.position);

            GameController.Instance.SetInHome();

            RevealPathBetweenButtons(GameController.Instance.PlayerNowPosition,homeButton.transform.position);

            if (GameController.Instance.Actions <= 0)
            {
                if (playerNowPointTemp != null)
                {
                    Destroy(playerNowPointTemp);
                }
                return;
            }
            
            SceneManager.LoadScene(3);
        }
        else
        {
            GameController.Instance.AddFlag("巡逻");//加上巡逻标志，play场景会通过查询标志来得到是否是在巡逻
            GameController.Instance.PropsFromBackpackToWarehouse();//巡逻前将背包中道具放入仓库中
            SceneManager.LoadScene(3);
        }
    }

    public void ConfirmCarryFoodsButtonDown()//确认带走食物出发按钮点击事件
    {
        if (float.TryParse(carryFoodsInputField.text, out float carryFoodsNum))//将输入栏的string类型转成float类型
        {
            float temp = (int)(carryFoodsNum * 10f) / 10f;//保留一位小数
            if (temp > GameController.Instance.FoodsInHome)//判断是否没有那么多食物带出
            {
                GiveATip("没有这么多食物吧？");
            }
            else if (temp < 0)//判断是否带出负数食物
            {
                GiveATip("何意味");
            }
            else
            {
                GameController.Instance.SetFoodsToCarry(temp);
                if (!GameController.Instance.CountPortableSpaceHaveBeyond())//没超出空间
                {
                    GameController.Instance.ChangeFoodsInHome(-GameController.Instance.FoodsToCarry);
                    if (isCityButtonDown)//看是否是通过city按钮呼出
                    {//选择city路线的在家departure
                        SetActions(spawnTrans[GameController.Instance.CitySpawnPointNum].position);
                        if (GameController.Instance.Actions <= 0)
                        {
                            Debug.Log("太近了");
                            return;
                        }
                        GameController.Instance.SetOutHome();//只要点击了这个按钮必然会离家
                        GameController.Instance.SetLastStep();//设置最后一步
                        SceneManager.LoadScene(3);//带着最后一步进行PLAY场景
                    }
                    else
                    {
                        Departure();
                    }
                }
                else
                {
                    GiveATip("没有这么多空间，请整理物品");
                }
            }
        }
        else
        {
            GiveATip("并非正常数字");
        }
    }

    public void OpenCarryFoodsBarAndSetNum(float needFoodsNum)//打开携带食物栏并设置已有食物和预期需要食物及能携带空间
    {
        carryFoodsBar.SetActive(true);//显示携带栏
        ShowBackgroundBlocker();
        portableSpaceNumText.text = GameController.Instance.PortableSpace.ToString();
        ownFoodsNumText.text = GameController.Instance.FoodsInHome.ToString();//从游戏控制器拿到所拥有食物总量
        needFoodsNumText.text = needFoodsNum.ToString();//初始化需要食物
    }

    public void CarryFoodsBarExitButtonDown()//携带食物栏的关闭按钮点击事件
    {
        carryFoodsBar.SetActive(false);
        isCityButtonDown = false;//既然关闭了窗口就不应该是city路线了
        HideBackgroundBlocker();
    }

    public void Departure()//出发，进入下一个场景
    {
        SetActions(new Vector3(mousePosition.x, mousePosition.y, 0));
        if (GameController.Instance.Actions <= 0)
        {
            GiveATip("我奶奶都能走过去");
            return;
        }
        if (GameController.Instance.IsInHome)//添加是否在家判断，如果在家就说明是从carryfoods窗口引用，反而就是从target按钮按下引用
        {
            RevealPathBetweenButtons(homeButton.transform.position,mousePosition);
        }
        else
        {
            RevealPathBetweenButtons(GameController.Instance.PlayerNowPosition,mousePosition);
        }
        GameController.Instance.SetOutHome();//只要点击了这个按钮必然会离家，注意这个代码要在生成步数的下面，不然无法生成步数
        GameController.Instance.SetPlayerNowPosition(new Vector3(mousePosition.x, mousePosition.y, 0));//给controlle传输目标位置，以便下次打开地图显示所在位置
        SceneManager.LoadScene(3);//跳转场景到Play场景
    }

    public void GiveATip(string tip)//在用户做出非法操作时蹦出来提示，tip参数是提示
    {
        tipBar.SetActive(true);//将提示栏显现出来
        tipBar.transform.Find("Tips").transform.Find("TipsContent").GetComponent<Text>().text = tip;//将提示给提示文本,这个代码纯整活玩法
        ShowBackgroundBlocker();
    }

    public void ConfirmTipsButtonDown()//提示栏按钮按下后
    {
        tipBar.SetActive(false);
        HideBackgroundBlocker();
    }

    public void ShowBackgroundBlocker()//显示遮盖文本，以防弹窗后点击背景触发事件
    {
        backgroundBlocker.SetActive(true);//背景遮盖打开
    }

    public void HideBackgroundBlocker()//隐藏遮盖文本，一般在关闭弹窗时使用
    {
        backgroundBlocker.SetActive(false);
    }

    public void SetFogImage()//设置迷雾图片，将纹理加在迷雾图片上，就相当于把画画在画纸上
    {
        GameController.Instance.SetFogTexture(fogTexture);//用于在控制器存储纹理，防止下次进入map无法记录上已经调查过的地区
        fogImage.texture = fogTexture;
    }

    public void SetupBackgroundClickHandling()//防止背景阻塞鼠标点击
    {
        background.GetComponent<Image>().raycastTarget=false;
    }

    public void SetupFogClickHandling()//设置迷雾点击处理，确保迷雾不阻塞鼠标点击
    {
        fogImage.raycastTarget = false; //使迷雾不会接收点击
    }

    public void DrawCircle(Texture2D tex, int cx, int cy, int r, Color color)//在纹理画圆来显示已探查地点
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r * r)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }
        if (cityButtonTemp!=null)
        {
            AfterFogUpdate();
        }
    }

    public void UpdateFog(Vector2 position,float revealRadiusTemp)//加载迷雾
    {
        // 在纹理上绘制圆形（透明区域）;
        Vector2 pixelUV = UIPositionToUV(position);
        int centerX = (int)(pixelUV.x * fogTexture.width);
        int centerY = (int)(pixelUV.y * fogTexture.height);
        int radiusPixels = (int)(revealRadiusTemp * fogTexture.width / 10f);

        DrawCircle(fogTexture, centerX, centerY, radiusPixels, new Color(0, 0, 0, 0));//画圆
        fogTexture.Apply();
    }

    public Vector2 UIPositionToUV(Vector2 uiScreenPos)//将ui屏幕坐标变成像素坐标
    {
        float u = uiScreenPos.x / Screen.width;
        float v = uiScreenPos.y / Screen.height;
        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    public void RevealPathBetweenButtons(Vector2 startUIPos,Vector2 endUIPos)//用来将两个地方之间的迷雾揭露出来
    {
        Vector2 stratUV = UIPositionToUV(startUIPos);
        Vector2 endUV = UIPositionToUV(endUIPos);

        float screenDistance = Vector2.Distance(startUIPos, endUIPos);//计算两点之间距离
        int circleCount = Mathf.Max(3,(int)(screenDistance/20f));//通过距离来算要找几个点
        for (int i = 0; i < circleCount; i++)//循环画圆揭露迷雾
        {
            float t = (float)i / circleCount;
            Vector2 midUV = Vector2.Lerp(stratUV,endUV,t);
            int centerX = (int)(midUV.x * fogTexture.width);
            int centerY = (int)(midUV.y * fogTexture.height);
            int radiusPixels = (int)(UnityEngine.Random.Range(playerRevealRadius-0.05f,playerRevealRadius+0.05f) * fogTexture.width / 10f);
            DrawCircle(fogTexture, centerX, centerY, radiusPixels, new Color(0, 0, 0, 0));//画圆
        }
        fogTexture.Apply();
    }

    public void AfterFogUpdate()//用来防止没有揭露城市的迷雾就能透过迷雾点击城市
    {
        Vector2 uv = UIPositionToUV(cityButtonTemp.transform.position);
        int x = (int)(uv.x*fogTexture.width);
        int y = (int)(uv.y*fogTexture.height);
        Color pixel = fogTexture.GetPixel(x,y);//查看按钮上的迷雾纹理是否透明
        cityButtonTemp.GetComponent<Button>().interactable = pixel.a < 0.3f;
    }

    public void ManagePropsButtonDown()//物品管理按钮按下触发事件,按下后弹出管理道具栏弹窗
    {
        managePropsBar.SetActive(true);
    }

    public void ManagePropsConfirmButtonDown()//管理道具栏弹窗中的唯一确定栏
    {
        managePropsBar.SetActive(false);
    }

    public void ContinuePrintManagePropsBarText()//持续打印管理物品栏的东西
    {
        float temp;
        if (float.TryParse(carryFoodsInputField.text, out float carryFoodsNum))//将输入栏的string类型转成float类型
        {
            carryFoodsNum = (int)(carryFoodsNum * 10f) / 10f;//保留一位小数
            if (carryFoodsNum > GameController.Instance.FoodsInHome)//判断是否没有那么多食物带出
            {
                temp = 0;
            }
            else if (carryFoodsNum < 0)//判断是否带出负数食物
            {
                temp = 0;
            }
            else//正常数字
            {
                temp = carryFoodsNum;
            }
        }
        else
        {
            temp = 0;
        }
        remainingSpaceNumText.text = (GameController.Instance.PortableSpace - temp).ToString();
    }

    public void GeneratePropsInWarehouseBar()//生成仓库栏的道具ui
    {
        foreach (Transform child in warehouseBarPropsContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var prop in GameController.Instance.PropsPoolInHome)
        {
            GameObject propUI = Instantiate(warehouseBarPropsUIPre, warehouseBarPropsContainer);
            propUI.GetComponent<WarehouseBarPropUI>().SetUI(prop);
        }
    }

    public void GeneratePropsInBackpackBar()//生成背包栏的道具ui
    {
        foreach (Transform child in backpackBarPropsContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var prop in GameController.Instance.PropsPoolOutside)
        {
            GameObject propUI = Instantiate(backpackBarPropsUIPre, backpackBarPropsContainer);
            propUI.GetComponent<BackpackBarPropUI>().SetUI(prop);
        }
    }

    public void RefreshProps()//刷新仓库和背包道具ui
    {
        if (GameController.Instance.CanRefreshProps)
        {
            GeneratePropsInWarehouseBar();
            GeneratePropsInBackpackBar();
            GameController.Instance.ChangeCanRefreshPropsToFalse();
        }
    }

    public void RadioEvent()//电台其三做完之后的后续
    {
        if ((GameController.Instance.FindPropsInHome("损坏的电台")||GameController.Instance.FindPropsOutside("损坏的电台"))&&GameController.Instance.IsInHome)
        { 
            int temp = 0;
            if (GameController.Instance.FindPropsInHome("修理工具"))
            {
                foreach (Prop p in GameController.Instance.PropsPoolInHome)
                {
                    if (p.propName == "修理工具")
                    {
                        temp += p.propCount;
                    }
                }
            }
            if (GameController.Instance.FindPropsOutside("修理工具"))
            {
                foreach (Prop p in GameController.Instance.PropsPoolOutside)
                {
                    if (p.propName == "修理工具")
                    {
                        temp += p.propCount;
                    }
                }
            }
            if (temp>=10)
            {
                GiveATip("电台已经修好，揭示了幸存区的位置" + "\n" + "大致在"+GameController.Instance.CityDirection+"方向上");
                GameController.Instance.ReducePropInHome("损坏的电台",1);
                GameController.Instance.ReducePropOutside("损坏的电台",1);
                GameController.Instance.AddPropInHome("电台",8,1);
            }
            else
            {
                GiveATip("大概需要十个修理工具才能修好损坏的电台");
            }
        }
    }

    public void RecoverAfterReturningHome()//回家后恢复
    {
        if (GameController.Instance.IsInHome)
        {
            GameController.Instance.ChangePlayerSan(5);
            GameController.Instance.ChangeHp(20);
        }
    }

    public void CheckSelectFlag()//检查是否有“抉择”flag，这个flag是用于Play的一个防止重复调用的条件，结束抉择事件后应该移除该flag，想了想还是放在map中恢复保险一点
    {
        if (GameController.Instance.HasFlag("抉择"))
        {
            GameController.Instance.DeleteFlag("抉择");
        }
    }
}
