using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class PlayManager : MonoBehaviour
{
    [SerializeField] private Animator animator;//抖动背景的动画机

    [SerializeField] private Text eventText;//事件发生说明栏的Text组件

    [SerializeField] private Text situationText;//根据发生的事情来进行描述的文本

    [SerializeField] private Text propsFoodNumText;//道具栏的食物数量文本

    [SerializeField] private Text stateSpiritText;//精神状态栏文本

    [SerializeField] private Text stateHpText;//状态栏血量文本

    [SerializeField] private Text selectSituationText;//选择栏的描述文本

    [SerializeField] private Text propsFoodNeedSpaceNumText;//道具栏的食物所占用携带空间数量文本

    [SerializeField] private Text propsNeedSpaceNumText;//道具栏所需要空间大小文本

    [SerializeField] private Text propsHaveSpaceNumText;//道具栏已有空间大小文本

    [SerializeField] private Text propsDiscardBeyondSpaceNumText;//丢弃物品栏中已超出空间文本

    [SerializeField] private Text propsDiscardFoodsNumText;//丢弃物品栏中显示食物所拥有数量

    [SerializeField] private Text propsDiscardFoodsNeedSpaceNumText;//丢弃物品栏中显示食物所需要占用空间数量

    [SerializeField] private InputField propsDiscardFoodsDiscardInputField;//丢弃物品栏中用于得到用户输入丢弃多少食物的输入框

    [SerializeField] private int actions;//根据上一个场景的距离来判断行走步数

    [SerializeField] private ScrollRect scrollRect;//可滑动组件

    [SerializeField] private Image backgroundBlocker;//背景遮盖

    [SerializeField] private GameObject propsDiscardBar;//丢弃物品栏

    [SerializeField] private GameObject propsDiscardUIPre;//丢弃道具栏的道具ui预制体

    [SerializeField] private GameObject propsUIPre;//道具栏的道具ui预制体

    [SerializeField] private Transform propsDiscardContainer;//丢弃道具栏的道具ui预制体的容纳箱

    [SerializeField] private Transform propsContainer;//道具栏的道具ui预制体的容纳箱

    [SerializeField] private List<GameEvent> eventPool = new List<GameEvent>();//事件池


    private Coroutine myCoroutine;//用于保存打字协程，方便后面关闭

    private bool isDotCycle = false;//是否进入点号循环

    private bool canShakeBack = true;//是否能抖动

    private bool isPaused = false;//是否暂停抽取

    private bool canNextScene = false;//是否可以进入下一个场景

    private bool isNewLineAdded = false;//判断是否有新一行文本加入

    private bool isDead = false;//判断是否死亡

    private float previousContentHeight;//保存文本原本高度，用于后面变化时做对照

    private float foodConsumputionPerStep = -0.5f;//设定每一步所消耗食物

    private float fadeDuration = 3f;//背景遮罩覆盖过渡时间

    private GameEvent gameEvent;//用于临时承载目前执行的事件

    private bool isYes = false;//监控选择按钮Yes按下

    private bool isNo = false;//监控选择按钮No按下

    void Start()
    {
        InitializeEvents();
        previousContentHeight = scrollRect.content.rect.height;
        if (GameController.Instance != null)//防止报空出错
        {
            if (GameController.Instance.HasFlag("巡逻"))
            {
                actions = 10;
            }
            else
            {
                actions = GameController.Instance.Actions;//调用游戏控制器来得到所要走的步数
            }
            GameController.Instance.CountPortableSpaceHaveBeyond();//play界面中需要打印已占用空间，需要该方法先走一步算出needspace的大小，不然会报空。
        }
        StartCoroutine(EventMachineCoroutine());
        stateSpiritText.text = stateSpiritText.text.Replace("2", "");
    }

    void Update()
    {
        GameController.Instance.CountPortableSpaceHaveBeyond();
        PrintStateSpiritText();
        PrintStateHpText();
        ShakeBackground();
        PrintDot();
        EventsTextHeightChange();
        DisplayPropsBarText();
        ChangePropsDiscardBeyondSpaceNumText();
    }
    public void DisplayPropsBarText()//道具栏的文本打印及其变色
    {
        propsFoodNumText.text = GameController.Instance.FoodsToCarry.ToString();
        propsFoodNeedSpaceNumText.text = GameController.Instance.FoodsToCarry.ToString();
        propsHaveSpaceNumText.text = GameController.Instance.PortableSpace.ToString();
        propsNeedSpaceNumText.text = GameController.Instance.NeedSpace.ToString();
        if (float.Parse(propsNeedSpaceNumText.text) > float.Parse(propsHaveSpaceNumText.text))
        {
            propsNeedSpaceNumText.color = Color.red;
        }
        else
        {
            propsNeedSpaceNumText.color = Color.black;
        }
        GenerateProps();
    }

    public void ShakeBackground()//抖动背景
    {
        if (canShakeBack)//如果不处于颤抖背景时
        {
            animator.SetBool("IsShake", true);   //设置参数开始抖动     
        }
    }

    public void PrintDot()//打印点号的方法
    {
        if (eventText.text.EndsWith("行进中...") && !isDotCycle)//判断是否是以特殊字符结尾，并且只需开启一次，因为协程中有一个死循环，无限打点号
        {
            isDotCycle = true;//只执行一次，关闭上面的条件
            eventText.text = eventText.text.TrimEnd('.');//把最后的点去除再把它赋值给Text文本
            myCoroutine = StartCoroutine(TypeDotCoroutine(eventText.text));//开启协程
        }
    }

    IEnumerator TypeDotCoroutine(string words)//辅助打印点号的协程
    {
        string dots = "...";//设置一个点
        while (true)//死循环
        {
            foreach (char t in dots)//遍历三个点
            {
                eventText.text += t;//给头秃了的Text文本加上一点
                yield return new WaitForSeconds(0.7f);//等待0.7秒
            }
            eventText.text = words;//重置Text文本，给text剃光头
        }
    }

    IEnumerator EventMachineCoroutine()//进行随机抽取来找事件
    {
        for (int i = 0; i < actions; i++)//通过步数来进行循环，看进行多少次的抽取
        {
            while (isPaused)//用来暂停抽取，isPaused与按钮相关，按钮回正，选择事件方法变负
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2);//用来给玩家一个空隙时间
            gameEvent = GetRandomEventWithCanTrigger();
            if (i == actions - 1)//判断是否走完，走完就可以进行下一个场景
            {
                canNextScene = true;//能进行下一个场景，变负
            }
            if (!isDead)//防止出错，虽然我也不觉得有什么用
            {
                if (GameController.Instance.HasFlag("巡逻"))
                {
                    GameController.Instance.ChangeFoodsToCarry(0);
                }
                else
                {
                    GameController.Instance.ChangeFoodsToCarry(foodConsumputionPerStep);
                }
                SelectEvents(gameEvent.eventName);
            }
        }
    }

    public void SelectEvents(string eventName)//选择事件
    {
        if (!isDead)//放出错，虽然我也不知道有什么用
        {
            switch (eventName)//根据抽签选择事件
            {
                case "无事发生":
                    NoEvent();
                    break;
                case "发现其一":
                    CommonEventDiscover1();
                    break;
                case "发现其二":
                    CommonEventDiscover2();
                    break;
                case "发现其三":
                    CommonEventDiscover3();
                    break;
                case "发现其四":
                    CommonEventDiscover4();
                    break;
                case "遭遇":
                    CommonEventEncounter();
                    break;
                case "巡逻其一":
                    PatrolEvent1();
                    break;
                case "巡逻其二":
                    PatrolEvent2();
                    break;
                case "巡逻其三":
                    PatrolEvent3();
                    break;
                case "巡逻无事发生":
                    PatrolNoEvent();
                    break;
                case "电台其一":
                    RadioEvent1();
                    break;
                case "电台其二":
                    RadioEvent2();
                    break;
                case "电台其三":
                    RadioEvent3();
                    break;
                case "幸存者":
                    SurvivalEvent();
                    break;
            }
        }
    }

    public void StopTypeDotAndShake()//停止抖动和打点
    {
        isDotCycle = false;//不在打点，可以进行是否打点判断
        if (myCoroutine != null)//防报空
        {
            StopCoroutine(myCoroutine);//停止打点
        }
        myCoroutine = null;//清除协程
        animator.SetBool("IsShake", false);//停止颤抖动画
        canShakeBack = false;//不能抖动
    }

    public void ConfirmButtonDown()//按下确认按钮
    {
        if (isDead)//进入死亡过程
        {
            SetMask();
            StartMask();
            return;
        }
        if (GameController.Instance.CountPortableSpaceHaveBeyond())
        {
            propsDiscardBar.SetActive(true);
            InitializeDiscardPropsBar();
            return;
        }
        if (GameController.Instance.HasFlag("抉择之后"))
        {
            GameController.Instance.DeleteFlag("抉择之后");
            SetMask();
            StartMask();
            GameController.Instance.SetInHome();//服务于后面的跳转场景方法,需要给一个回家的处理
            canNextScene = true;//服务于后面的跳转场景方法
            if (GameController.Instance.HasFlag("女主"))//如果是女主直接初始化状态
            {
                GameController.Instance.ChangePlayerSan(15);
                GameController.Instance.ChangeHp(100);
            }
            Invoke(nameof(NextIsMapOrSummarySceneOrGoOn), 4f);
            GameController.Instance.ChangePortableSpace(-10);
            return;
        }

        situationText.transform.parent.gameObject.SetActive(false);//隐藏情况栏
        canShakeBack = true;//能抖动
        isPaused = false;//取消暂停限制
        NextIsMapOrSummarySceneOrGoOn();
    }

    public void NextIsMapOrSummarySceneOrGoOn()//用于判断下一步是继续行走还是结束游戏还是进入地图
    {
        if (GameController.Instance.IsLastStep && canNextScene)//判断是否达成结束游戏条件
        {
            SetMask();
            StartMask();
        }
        if (canNextScene && !GameController.Instance.IsLastStep)//是否进入下一个场景
        {
            if (GameController.Instance.IsInHome)//如果是点击了Home按钮完成了回家操作，则家里的食物的数量就要把所携带食物和家里的食物加在一起
            {
                GameController.Instance.ChangeFoodsInHome(GameController.Instance.FoodsToCarry);
                GameController.Instance.SetFoodsToCarry(0);
            }
            if (GameController.Instance.HasFlag("巡逻"))//如果是在巡逻
            {
                GameController.Instance.DeleteFlag("巡逻");//去除巡逻标志，结束巡逻
            }
            if (GameController.Instance.HasFlag("双人日记"))//如果收养了在此次行程里收养了小女孩
            {
                SceneManager.LoadScene(1);
            }
            else
            {
                SceneManager.LoadScene(2);
            }
        }
        else
        {
            eventText.text += "\n行进中...";
        }
    }

    public void EventsTextHeightChange()//用来检测实际文本行数是否变化
    {
        if (Mathf.Abs(previousContentHeight - scrollRect.content.rect.height) > 0.1f)
        {
            isNewLineAdded = true;
            previousContentHeight = scrollRect.content.rect.height;
        }
        if (isNewLineAdded)
        {
            ScrollToButtom();
            isNewLineAdded = false;
        }
    }

    public void ScrollToButtom()//将Scroll的Content的最下层移动到scroll的最下层
    {
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void YesButtonDown()//Yes按钮按下事件
    {
        selectSituationText.transform.parent.gameObject.SetActive(false);
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isYes = true;
    }

    public void NoButtonDown()//No按钮按下事件
    {
        selectSituationText.transform.parent.gameObject.SetActive(false);
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isNo = true;
    }
    public void InitializeEvents()//初始化事件
    {
        eventPool.Clear();
        eventPool.Add(new GameEvent()
        {
            eventName = "无事发生",
            weight = 60,
            description = "什么事情都没有发生，对应NoEvent()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "发现其一",
            weight = 5,
            description = "找到了点东西，搜刮了停在路边的一辆小车，对应CommentDiscover1()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "发现其二",
            weight = 6,
            description = "找到了点东西，掏了掏路边的果子，对应CommentDiscover2()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "发现其三",
            weight = 9,
            description = "找到了点东西，掏了掏树上的鸟窝，对应CommentDiscover3()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "发现其四",
            weight = 4,
            description = "找到了点东西，路边有一个小卖部，对应CommentDiscover4()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );

        eventPool.Add(new GameEvent()
        {
            eventName = "遭遇",
            weight = 11,
            description = "前方有点动静，对应CommonEventEncounter1()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "巡逻其一",
            weight = 19,
            description = "搜寻了陷阱中的行尸，对应PatrolEvent1()",
            requiredFlags = new List<string> { "活着", "巡逻" },
            blockingFlags = new List<string> { "无" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "巡逻其二",
            weight = 19,
            description = "清理了仓库，对应PatrolEvent2()",
            requiredFlags = new List<string> { "活着", "巡逻" },
            blockingFlags = new List<string> { "无" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "巡逻其三",
            weight = 2,
            description = "收获菜园，对应PatrolEvent3()",
            requiredFlags = new List<string> { "活着", "巡逻" },
            blockingFlags = new List<string> { "无" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "巡逻无事发生",
            weight = 70,
            description = "什么都没干，对应PatrolNoEvent()",
            requiredFlags = new List<string> { "活着", "巡逻" },
            blockingFlags = new List<string> { "无" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "电台其一",
            weight = 4,
            description = "前面有一只饥肠辘辘的小猫，对应RadioEvent1()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻", "电台其一" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "电台其二",
            weight = 4,
            description = "又遇到了那只小猫，对应RadioEvent2()",
            requiredFlags = new List<string> { "活着", "电台其一" },
            blockingFlags = new List<string> { "无", "巡逻", "电台其二" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "电台其三",
            weight = 4,
            description = "有一个奇怪的房子，小猫趴在门口叫唤，对应RadioEvent3()",
            requiredFlags = new List<string> { "活着", "电台其二" },
            blockingFlags = new List<string> { "无", "巡逻", "电台其三" },
        }
        );
        eventPool.Add(new GameEvent()
        {
            eventName = "幸存者",
            weight = 1,
            description = "几只行尸围着一个地下室，对应SurvivalEvent()",
            requiredFlags = new List<string> { "活着" },
            blockingFlags = new List<string> { "无", "巡逻", "双人", "男主", "女主" },
        }
        );
    }
    public void CommonEventDiscover1()
    {
        StopTypeDotAndShake();
        if (GameController.Instance.HasFlag("女主"))
        {
            eventText.text = eventText.text + "\n" + "应该能找到点东西";//给事件栏加文本
            situationText.text = "一辆小车?我应该找找看有什么能用得上的东西" + "\n" + "食物+1";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(1);//用于改变所携带食物数量
        }
        else
        {
            eventText.text = eventText.text + "\n" + "找到了点东西";//给事件栏加文本
            situationText.text = "路边停着一辆小车，或许我能从里面找到什么东西" + "\n" + "食物+2";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(2);//用于改变所携带食物数量
        }
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
    }
    public void CommonEventDiscover2()
    {
        StopTypeDotAndShake();
        if (GameController.Instance.HasFlag("女主"))
        {
            eventText.text = eventText.text + "\n" + "应该能找到点东西";//给事件栏加文本
            situationText.text = "这个果子?我见叔叔摘过！" + "\n" + "食物+2";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(2);//用于改变所携带食物数量
        }
        else
        {
            eventText.text = eventText.text + "\n" + "找到了点东西";//给事件栏加文本
            situationText.text = "这个果子看着应该能吃吧，摘一点吧" + "\n" + "食物+1";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(1);//用于改变所携带食物数量
        }
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
    }
    public void CommonEventDiscover3()
    {
        StopTypeDotAndShake();
        if (GameController.Instance.HasFlag("女主"))
        {
            eventText.text = eventText.text + "\n" + "应该能找到点东西";//给事件栏加文本
            situationText.text = "鸟窝诶，听叔叔说鸟窝里可能有鸟蛋" + "\n" + "食物+0.5";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(0.5f);//用于改变所携带食物数量
        }
        else
        {
            eventText.text = eventText.text + "\n" + "找到了点东西";//给事件栏加文本
            situationText.text = "树上有鸟窝，说不定会有鸟蛋" + "\n" + "食物+0.5";//给情况栏加文本
            GameController.Instance.ChangeFoodsToCarry(0.5f);//用于改变所携带食物数量
        }
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
    }
    public void CommonEventDiscover4()
    {
        StopTypeDotAndShake();
        if (GameController.Instance.HasFlag("女主"))
        {
            eventText.text = eventText.text + "\n" + "应该能找到点东西";//给事件栏加文本
            selectSituationText.text = "是个小卖部，要去看看吗";//给选择栏描述文本加文本
        }
        else
        {
            eventText.text = eventText.text + "\n" + "找到了点东西";//给事件栏加文本
            selectSituationText.text = "那好像是个小卖部，要冒险去探探吗";//给选择栏描述文本加文本
        }
        selectSituationText.transform.parent.gameObject.SetActive(true);//让选择栏弹出
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
        StartCoroutine(nameof(IEOfCommonEventDiscover4));
    }
    IEnumerator IEOfCommonEventDiscover4()//发现其四附加的协程
    {
        while (true)
        {
            if (isYes)
            {
                int random = Random.Range(0,3);
                switch (random)
                {
                    case 0:
                    case 1:
                        if (GameController.Instance.HasFlag("女主"))
                        {
                            situationText.text = "大丰收！！！看来暂时不愁吃的了呢" + "\n" + "食物+5";//给情况栏加文本
                            GameController.Instance.ChangeFoodsToCarry(5);
                        }
                        else
                        {
                            situationText.text = "暂时不愁吃了" + "\n" + "食物+5";//给情况栏加文本
                            GameController.Instance.ChangeFoodsToCarry(5);
                        }
                        break;
                    case 2:
                        if (GameController.Instance.HasFlag("女主"))
                        {
                            situationText.text = "好险，还好足够谨慎" + "\n" + "食物+3";//给情况栏加文本
                            GameController.Instance.ChangeFoodsToCarry(3);
                        }
                        else
                        {
                            situationText.text = "差点栽在里头" + "\n" + "食物+1";//给情况栏加文本
                            GameController.Instance.ChangeFoodsToCarry(1);
                            GameController.Instance.ChangeHp(-20);
                        }
                        break;
                }
                isYes = false;
                yield break;
            }
            if (isNo)
            {
                if (GameController.Instance.HasFlag("女主"))
                {
                    situationText.text = "里面好暗，还是不进去了吧";
                }
                else
                {
                    situationText.text = "为了安全起见还是不进去了吧";//给情况栏加文本
                }
                isNo = false;
                yield break;
            }
            yield return null;
        }
    }

    public void SurvivalEvent()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "旁边的房子里有点声响";//给事件栏加文本
        selectSituationText.transform.parent.gameObject.SetActive(true);//让选择栏弹出
        selectSituationText.text = "几只行尸正围着一个地下室,要去看看什么情况吗";//给选择栏描述文本加文本
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
        StartCoroutine(nameof(IEOfSurvivalEvent));
    }

    IEnumerator IEOfSurvivalEvent()
    {
        while (true)
        {
            if (isYes)
            {
                if (GameController.Instance.FindPropsOutside("猎枪"))
                {
                    situationText.text = "猎枪-1" + "\n"+"打开地下室，一个小女孩待在下面"+"\n"+"“现在外面太危险了，你还是跟着我吧”";//给情况栏加文本
                    GameController.Instance.AddFlag("双人");
                    GameController.Instance.ReducePropOutside("猎枪",1);
                    GameController.Instance.AddFlag("双人日记");
                    GameController.Instance.ChangePortableSpace(10);
                }
                else
                {
                    situationText.text = "受了点伤" + "\n" + "打开地下室，一个小女孩待在下面" + "\n" + "“现在外面太危险了，你还是跟着我吧”";//给情况栏加文本
                    GameController.Instance.AddFlag("双人");
                    GameController.Instance.ChangeHp(-50);
                    GameController.Instance.AddFlag("双人日记");
                    GameController.Instance.ChangePortableSpace(10);
                }
                isYes = false;
                yield break;
            }
            if (isNo)
            {
                situationText.text = "没有必要浪费时间吧";//给情况栏加文本
                isNo = false;
                yield break;
            }
            yield return null;
        }
    }

    public void RadioEvent1()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "前面有什么东西";//给事件栏加文本
        selectSituationText.transform.parent.gameObject.SetActive(true);//让选择栏弹出
        selectSituationText.text = "有一只饥肠辘辘的小猫趴在路上,要分一点食物给它吗";//给选择栏描述文本加文本
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
        StartCoroutine(nameof(IEOfRadioEvent1));
    }
    IEnumerator IEOfRadioEvent1()
    {
        while (true)
        {
            if (isYes)
            {
                if (GameController.Instance.FoodsToCarry > 3)
                {
                    if (GameController.Instance.HasFlag("女主"))
                    {
                        situationText.text = "它看起来很开心呢，喵喵~" + "\n" + "食物-3";//给情况栏加文本
                    }
                    else
                    {
                        situationText.text = "摸了摸它的头" + "\n" + "食物-3";
                    }
                    GameController.Instance.ChangeFoodsToCarry(-3);
                    GameController.Instance.AddFlag("电台其一");
                }
                else
                {
                    situationText.text = "食物好像不够多，走吧";//给情况栏加文本
                }
                isYes = false;
                yield break;
            }
            if (isNo)
            {
                if (GameController.Instance.HasFlag("女主"))
                {
                    situationText.text = "食物不够多呢";//给情况栏加文本
                }
                else
                {
                    situationText.text = "食物很宝贵";//给情况栏加文本
                }
                isNo = false;
                yield break;
            }
            yield return null;
        }
    }

    public void RadioEvent2()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "前面有什么东西";//给事件栏加文本
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        situationText.text = "又是那只猫，它嘴里好像叼着什么东西" + "\n" + "奇怪的钥匙+1";//给情况栏加文本
        GameController.Instance.AddPropOutside("奇怪的钥匙", 1, 1);
        GameController.Instance.AddFlag("电台其二");
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
    }

    public void RadioEvent3()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "前面有个奇怪的房子";//给事件栏加文本
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        if (GameController.Instance.FindPropsOutside("奇怪的钥匙"))
        {
            situationText.text = "那只小猫趴在上锁的门口" + "\n" + "奇怪的钥匙-1" + "\n" + "损坏的电台+1" + "\n" + "电台修好了似乎能向外发送救援信号，带回家去修吧";//给情况栏加文本
            GameController.Instance.ReducePropOutside("奇怪的钥匙", 1);
            GameController.Instance.AddPropOutside("损坏的电台", 8, 1);
            GameController.Instance.AddFlag("电台其三");
        }
        else
        {
            situationText.text = "门上锁了，开锁工具好像开不了";//给情况栏加文本
        }
        isPaused = true;
    }
    public void CommonEventEncounter()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "前面似乎有点动静";
        situationText.transform.parent.gameObject.SetActive(true);
        if (GameController.Instance.FindPropsOutside("猎枪"))
        {
            int random = Random.Range(0, 3);
            switch (random)
            {
                case 0:
                case 1:
                    situationText.text = "掏出猎枪保护了自己";
                    break;
                case 2:
                    situationText.text = "掏出猎枪保护了自己,但是猎枪也好像不能使用了" + "\n" + "猎枪-1";
                    GameController.Instance.ReducePropOutside("猎枪", 1);
                    break;
            }
        }
        else
        {
            situationText.text = "慌忙逃走" + "\n" + "食物-2";
            GameController.Instance.ChangeHp(-30);
            GameController.Instance.ChangeFoodsToCarry(-2);
        }
        isPaused = true;
    }
    public void NoEvent()//无事发生事件
    {
        if (myCoroutine != null)
        {
            StopCoroutine(myCoroutine);
        }
        myCoroutine = null;
        isDotCycle = false;//以上为需要抖动屏幕效果但不需要打点效果
        eventText.text = eventText.text + "\n" + "什么都没有发生";
        NextIsMapOrSummarySceneOrGoOn();
    }

    public void PatrolEvent1()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "搜刮了掉入陷阱的行尸";//给事件栏加文本
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isPaused = true;
        int r = Random.Range(0, 3);
        switch (r)
        {
            case 0:
                situationText.text = "它们身上说不定会有什么有用的道具" + "\n" + "开锁工具+1";//给情况栏加文本
                GameController.Instance.AddPropOutside("修理工具", 1, 1);
                break;
            case 1:
                situationText.text = "什么都没找到";//给情况栏加文本
                break;
            case 2:
                situationText.text = "找到了背包" + "\n" + "携带空间+3";
                GameController.Instance.ChangePortableSpace(3);
                break;
        }
    }

    public void PatrolEvent2()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "整理了仓库";//给事件栏加文本
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        isPaused = true;
        int r = Random.Range(0, 5);
        switch (r)
        {
            case 0:
                situationText.text = "发现了有用的东西" + "\n" + "修理工具+1";//给情况栏加文本
                GameController.Instance.AddPropOutside("修理工具", 1, 1);
                break;
            case 1:
                situationText.text = "这儿还有一些罐头" + "\n" + "食物+2";
                GameController.Instance.ChangeFoodsToCarry(2);
                break;
            case 2:
                situationText.text = "什么都没有";
                break;
            case 3:
                situationText.text = "找到了背包"+"\n"+"携带空间+3";
                GameController.Instance.ChangePortableSpace(3);
                break;
            case 4:
                situationText.text = "找到了猎枪" + "\n" + "猎枪+1";
                GameController.Instance.AddPropOutside("猎枪",3,1);
                break;
        }
    }

    public void PatrolEvent3()
    {
        StopTypeDotAndShake();
        eventText.text = eventText.text + "\n" + "收获了菜园";//给事件栏加文本
        situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
        situationText.text = "收获了辛苦种植的菜园" + "\n" + "食物+10";//给情况栏加文本
        GameController.Instance.ChangeFoodsToCarry(10);//用于改变所携带食物数量
        isPaused = true;//让随机暂停，用于让用户确认发生了什么情况
    }

    public void PatrolNoEvent()//无事发生事件
    {
        if (myCoroutine != null)
        {
            StopCoroutine(myCoroutine);
        }
        myCoroutine = null;
        isDotCycle = false;//以上为需要抖动屏幕效果但不需要打点效果
        eventText.text = eventText.text + "\n" + "巡视了一会儿，什么都没发生";
        NextIsMapOrSummarySceneOrGoOn();
    }

    public void PrintStateSpiritText()//打印精神状态栏状态
    {
        if (GameController.Instance.PlayerSan < 10 && GameController.Instance.PlayerSan >= 5)
        {
            stateSpiritText.text = "精神萎靡";
            GameController.Instance.AddFlag("精神萎靡");
        }
        else if (GameController.Instance.PlayerSan > 0 && GameController.Instance.PlayerSan < 5)
        {
            stateSpiritText.text = "濒临崩溃";
            GameController.Instance.AddFlag("濒临崩溃");
        }
        else if (GameController.Instance.PlayerSan <= 0)
        {
            //GameOver，精神崩溃结局
            StopTypeDotAndShake();
            situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
            situationText.text = "前面出现了两个人，我终于找到幸存者了！！！";//给情况栏加文本
            isDead = true;//已经死亡
        }
        else
        {
            GameController.Instance.DeleteFlag("精神萎靡");
            GameController.Instance.DeleteFlag("濒临崩溃");
            //精神正常，无需打印
        }
    }

    public void PrintStateHpText()//打印状态栏hp状态
    {
        if (GameController.Instance.PlayerSan <= 0)//防止两个死亡条件同时触发时死亡两次，丧值优先级设置高一点
        {
            return;
        }
        if (GameController.Instance.HasFlag("抉择"))//为了防止重复调用打印状态导致抉择过程丢失
        {
            return;
        }
        if (GameController.Instance.Hp < 75 && GameController.Instance.Hp >= 25)
        {
            stateHpText.text = "轻伤";
            GameController.Instance.AddFlag("轻伤");
        }
        else if (GameController.Instance.Hp > 0 && GameController.Instance.Hp < 25)
        {
            stateHpText.text = "重伤";
            GameController.Instance.AddFlag("重伤");
        }
        else if (GameController.Instance.Hp <= 0)
        {
            //GameOver，伤势无法挽救结局
            StopTypeDotAndShake();
            if (GameController.Instance.HasFlag("双人") && !GameController.Instance.HasFlag("双人日记"))//添加双人日记是为了防止看日记前就触发抉择
            {
                GameController.Instance.AddFlag("抉择");//为了防止重复调用打印状态导致抉择过程丢失
                selectSituationText.transform.parent.gameObject.SetActive(true);//让选择栏弹出
                selectSituationText.text = "面对围上来的行尸，你看了看小女孩，或许我们中还能活一个，是否要丢下她";//给选择栏描述文本加文本
                StartCoroutine(nameof(Servive));
            }
            else
            {
                situationText.transform.parent.gameObject.SetActive(true);//让情况栏弹出
                situationText.text = "两只行尸扑了上来...";//给情况栏加文本
                isDead = true;//已经死亡
            }
        }
        else
        {
            GameController.Instance.DeleteFlag("轻伤");
            GameController.Instance.DeleteFlag("重伤");
            //hp正常，无需打印
        }
    }
    IEnumerator Servive()//生存抉择
    {
        while (true)
        {
            if (isYes)//以后仍然为主角视角
            {
                situationText.text = "逃出来了";//给情况栏加文本
                GameController.Instance.DeleteFlag("双人");
                GameController.Instance.AddFlag("抉择之后");
                GameController.Instance.AddFlag("男主");
                isYes = false;
                yield break;
            }
            if (isNo)//以后为女孩视角
            {
                situationText.text = "逃了出来";//给情况栏加文本
                GameController.Instance.DeleteFlag("双人");
                GameController.Instance.AddFlag("抉择之后");//方便后面的条件判断，判断完直接删掉 
                GameController.Instance.AddFlag("女主");
                isNo = false;
                yield break;
            }
            yield return null;
        }
    }

    public void SetMask()//将背景遮罩显示出来虽然透明度为0
    {
        backgroundBlocker.gameObject.SetActive(true);
    }

    public void StartMask()//开始调整背景遮罩透明度，开启协程
    {
        StartCoroutine(FadeMask(0f, 1f));
    }

    IEnumerator FadeMask(float fromAlpha, float toAlpha)//增大背景遮罩透明度
    {
        Color color = backgroundBlocker.color;//声明一个临时储存的color
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            backgroundBlocker.color = color;
            yield return null;
        }
        color.a = toAlpha;//直接到达目标透明度，防止出错
        backgroundBlocker.color = color;
        Invoke(nameof(NextSceneToSummary), 1f);//两秒后跳转场景
    }

    public void NextSceneToSummary()//跳转场景到Summary
    {
        SceneManager.LoadScene(4);
    }

    public GameEvent GetRandomEvent(List<GameEvent> availableEvents)//随机触发一个可触发事件，仅仅只是执行抽奖行为，不判断是否可以触发
    {
        int totalWeight = 0;
        foreach (GameEvent e in availableEvents)
        {
            totalWeight += e.weight;
        }
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        foreach (GameEvent e in availableEvents)
        {
            currentWeight += e.weight;
            if (randomValue < currentWeight)//抽中
            {
                return e;
            }
        }
        return null;//基本不会发生，只有池子里面没有事件的时候会返回空
    }

    public bool CanTriggerEvent(GameEvent e)//判断是否可以触发该事件，要得到事件的钥匙的标识和避免事件受锁的标识才能触发该事件
    {
        foreach (string flag in e.requiredFlags)//遍历事件钥匙标识，看现有标识是否拥有
        {
            if (!GameController.Instance.HasFlag(flag))
            {
                return false;
            }
        }
        foreach (string flag in e.blockingFlags)//遍历事件锁标识，看现有标识是否拥有
        {
            if (GameController.Instance.HasFlag(flag))
            {
                return false;
            }
        }
        return true;//两个条件都满足
    }

    public GameEvent GetRandomEventWithCanTrigger()//随机得到一个可触发事件，仅进行归纳符合条件的事件，不进行抽奖动作
    {
        List<GameEvent> availableEvents = new List<GameEvent>();//提供一个临时承载所有可以进行的事件的事件池，再在后面把它喂给抽奖方法
        foreach (var e in eventPool)
        {
            //检查条件
            if (!CanTriggerEvent(e))
            {
                continue;//不符合触发条件就进入下一个循环
            }
            //通过所有条件
            availableEvents.Add(e);
        }
        return GetRandomEvent(availableEvents);
    }

    public void ChangePropsDiscardBeyondSpaceNumText()//改变现在已超出空间大小文本，让用户看到改变
    {
        float temp = (GameController.Instance.NeedSpace - GameController.Instance.PortableSpace);//从控制器得到超过空间大小

        if (float.TryParse(propsDiscardFoodsDiscardInputField.text, out float discardFoods))//确保是合法数字,不是合法数字将不会进行操作
        {
            float discardFoodsTemp = (int)(discardFoods * 10f) / 10f;//保留一位小数
            if (discardFoodsTemp > GameController.Instance.FoodsToCarry)//如果大于已有食物
            {

            }
            else if (discardFoodsTemp < 0)//如果是小数
            {

            }
            else//正常数字
            {
                temp -= discardFoodsTemp;
            }
        }
        //以上if语句大致作用为，计算食物丢弃多少对于超出空间数量文本的改变

        //以下需要进行的操作主要是，计算道具丢弃多少对于超出空间数量文本的改变
        foreach (Transform childDiscardProp in propsDiscardContainer)
        {
            PropsDiscardBarPropUI uiTemp = childDiscardProp.GetComponent<PropsDiscardBarPropUI>();//取得子ui物体的ui脚本
            if (int.TryParse(uiTemp.propsDiscardPropDiscardInputField.text, out int discardProp))
            {
                if (discardProp > int.Parse(uiTemp.propsDiscardPropNumText.text))
                {

                }
                else if (discardProp < 0)
                {

                }
                else//正常数字
                {
                    temp -= discardProp * uiTemp.propsDiscardPropEachNeedSpaceNum;
                }
            }
        }

        if (temp < 0)//避免超出空间出现负数
        {
            temp = 0;
        }
        propsDiscardBeyondSpaceNumText.text = temp.ToString();
    }

    public void InitializeDiscardPropsBar()//初始化丢弃道具栏
    {
        propsDiscardFoodsNumText.text = GameController.Instance.FoodsToCarry.ToString();
        propsDiscardFoodsNeedSpaceNumText.text = GameController.Instance.FoodsToCarry.ToString();
        GenerateDiscardProps();
    }

    public void ConfirmDiscardPropsButtonDown()//确认丢弃按钮按下,主要是用于对控制器的变量进行赋值的改变，和ChangePropsDiscardBeyondSpaceNumText的区别主要是这个
    {
        float foodTemp = 0;
        if (float.TryParse(propsDiscardFoodsDiscardInputField.text, out float discardFoods))//确保是合法数字
        {
            foodTemp = (int)(discardFoods * 10f) / 10f;//保留一位小数
            if (foodTemp > GameController.Instance.FoodsToCarry)//如果大于已有食物
            {
                return;
            }
            else if (foodTemp < 0)//如果是负数
            {
                return;
            }
            //正常数字,仅仅只是用来看丢多少食物，判断是否可以继续行走是下面判断是否超出空间为0
        }
        else if (string.IsNullOrEmpty(propsDiscardFoodsDiscardInputField.text))
        {
            //GoOn
        }
        else
        {
            return;
        }
        //只有空字符串和null和正常数字能通过，其他直接跳出方法

        //以下操作作用是计算丢弃多少数量的道具
        foreach (Transform childDiscardProp in propsDiscardContainer)
        {
            PropsDiscardBarPropUI uiTemp = childDiscardProp.GetComponent<PropsDiscardBarPropUI>();//取得子ui物体的ui脚本
            if (int.TryParse(uiTemp.propsDiscardPropDiscardInputField.text, out int discardProp))
            {
                if (discardProp > int.Parse(uiTemp.propsDiscardPropNumText.text))
                {
                    return;
                }
                else if (discardProp < 0)
                {
                    return;
                }
            }
            else if (string.IsNullOrEmpty(uiTemp.propsDiscardPropDiscardInputField.text))
            {
                //GoOn
            }
            else
            {
                return;
            }
        }
        //只有空字符串和null和正常数字能通过，其他直接跳出方法

        if (float.Parse(propsDiscardBeyondSpaceNumText.text) > 0)//如果超出空间大于0就继续丢，如果没有就下一步
        {
            return;
        }
        //完成所有条件判断，允许丢弃

        foreach (Transform childDiscardProp in propsDiscardContainer)//丢弃道具
        {
            PropsDiscardBarPropUI uiTemp = childDiscardProp.GetComponent<PropsDiscardBarPropUI>();//取得子ui物体的ui脚本
            if (string.IsNullOrEmpty(uiTemp.propsDiscardPropDiscardInputField.text))//是空字符串或者null
            {
                GameController.Instance.ReducePropOutside(uiTemp.propsDiscardPropNameText.text, 0);
            }
            else
            {
                GameController.Instance.ReducePropOutside(uiTemp.propsDiscardPropNameText.text, int.Parse(uiTemp.propsDiscardPropDiscardInputField.text));
            }
        }
        GameController.Instance.ChangeFoodsToCarry(-foodTemp);//丢弃食物
        propsDiscardBar.SetActive(false);
    }

    public void GenerateDiscardProps()//生成丢弃道具栏的道具ui
    {
        foreach (Transform child in propsDiscardContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var prop in GameController.Instance.PropsPoolOutside)
        {
            GameObject propDiscardUI = Instantiate(propsDiscardUIPre, propsDiscardContainer);
            propDiscardUI.GetComponent<PropsDiscardBarPropUI>().SetUI(prop);
        }
    }

    public void GenerateProps()//生成道具栏的道具ui
    {
        foreach (Transform child in propsContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var prop in GameController.Instance.PropsPoolOutside)
        {
            GameObject propUI = Instantiate(propsUIPre, propsContainer);
            propUI.GetComponent<PropsBarPropUI>().SetUI(prop);
        }
    }
}

