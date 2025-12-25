using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance;//单例模式


    [SerializeField] private Vector3 playerNowPosition;//记录玩家所到达的位置
    public Vector3 PlayerNowPosition
    {
        get { return playerNowPosition; }
    }


    [SerializeField] private int actions;//通过地图设置目标点所算出来的步数
    public int Actions
    {
        get { return actions; }
    }


    [SerializeField] private int citySpawnPointNum;//记录城市生成点位的随机数
    public int CitySpawnPointNum
    {
        get { return citySpawnPointNum; }
    }


    [SerializeField] private bool isLastStep;//用于判断是否进行最后一步，即将跳转到结算场景
    public bool IsLastStep
    {
        get { return isLastStep; }
    }

    [SerializeField] private bool isFirstGame = true;//判断是否第一次进入游戏，在外面脚本进行初始化时可以调用
    public bool IsFirstGame
    {
        get { return isFirstGame; }
    }

    [SerializeField] private bool isInHome = true;//判断是否在家
    public bool IsInHome
    {
        get { return isInHome; }
    }

    [SerializeField] private float foodsInHome;//用于记录所拥有的粮食的总量
    public float FoodsInHome
    {
        get { return foodsInHome; }
    }

    [SerializeField] private float foodsToCarry;//用于记录出发时所携带的食物量
    public float FoodsToCarry
    {
        get { return foodsToCarry; }
    }

    [SerializeField] private float playerSan;//用于记录玩家丧值
    public float PlayerSan
    {
        get { return playerSan; }
    }

    [SerializeField] private float hp;//用于记录玩家血量
    public float Hp
    {
        get { return hp; }
    }

    [SerializeField] private Texture2D fogTexture;//用于保存已改变的迷雾纹理,提供全局纹理
    public Texture2D FogTexture
    {
        get {return fogTexture; }
    }

    [SerializeField] private List<string> flags =new List<string>();//用于存储所经历的标志
    public List<string> Flags
    {
        get { return flags; }
    }

    [SerializeField] private float portableSpace;//用于存储所能携带空间
    public float PortableSpace
    {
        get { return portableSpace; }
    }

    [SerializeField] private List<Prop> propsPoolOutside = new List<Prop>();//用于存储背包所拥有的道具(即携带道具)
    public List<Prop> PropsPoolOutside
    {
        get {return propsPoolOutside; }
    }
    
    [SerializeField] private List<Prop> propsPoolInHome = new List<Prop>();//用于存储仓库所拥有的道具(即未携带道具)
    public List<Prop> PropsPoolInHome
    {
        get {return propsPoolInHome; }
    }

    [SerializeField] private float needSpace;//用于存储所需要的空间大小
    public float NeedSpace
    {
        get { return needSpace; }
    }

    [SerializeField] private bool canRefreshProps =true;//用来判断是否需要刷新道具ui
    public bool CanRefreshProps
    {
        get {return canRefreshProps; }
    }

    [SerializeField] private string cityDirection;//用来存储城市方位，用于电台事件的幸存区位置揭露
    public string CityDirection
    {
        get { return cityDirection; }
    }
    private void Awake()
    {
        if (Instance == null)//实现单例
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetPlayerNowPosition(Vector3 playerPosition)//用来在其他脚本调用来设置目前位置
    {
        playerNowPosition = playerPosition;
    }

    public void SetActions(int actionsTemp)//用来在其他脚本调用来设置当前需要的步数
    {
        actions = actionsTemp;
    }

    public void SetCitySpawnPointNum(int citySpawnPointNumTemp)//用来在其他脚本调用来设置当前城市生成点位的随机数
    {
        citySpawnPointNum = citySpawnPointNumTemp;
    }

    public void SetLastStep()//用来在其他脚本调用来设置可以进行最后一步
    {
        isLastStep = true;
    }

    public void ChangeFoodsInHome(float changeFoods)//用来在其他脚本调用来进行粮食总量的改变
    {
        foodsInHome += changeFoods;
    }

    public void GetFirstGameResource()//用来设置第一次游戏的资源
    {
        playerSan = 15;
        hp = 100;
        foodsInHome = 20;
        isFirstGame = false;
        portableSpace = 10;
        AddFlag("活着");
        AddPropInHome("猎枪",3,1);
        AddPropInHome("修理工具",1,5);
    }

    public void SetOutHome()//用来设置离家
    {
        isInHome = false;
    }

    public void SetInHome()//用来设置回家
    {
        isInHome = true;
    }

    public void SetFoodsToCarry(float foodsToCarryTemp)//从其他脚本设置玩家出发时所携带的食物
    {
        foodsToCarry = foodsToCarryTemp;
    }

    public void ChangeFoodsToCarry(float foodsToCarryChange)//用来从其他脚本改变玩家所携带食物
    {
        if ((foodsToCarry+foodsToCarryChange)<0)//如果食物变化使所携带的食物减到了0以下
        {
            foodsToCarry = 0;
            ChangePlayerSan(-Mathf.Abs(foodsToCarry + foodsToCarryChange) * 1);
        }
        else
        {
            foodsToCarry += foodsToCarryChange;
        }
    }

    public void ChangePlayerSan(float playerSanChange)//从其他脚本来改变玩家丧值
    {
        playerSan += playerSanChange;
        if (playerSan>15)
        {
            playerSan = 15;
        }
        if (playerSan<0)
        {
            playerSan = 0;
        }
    }

    public void ChangeHp(float hpChange)//从其他脚本来改变玩家血量
    {
        hp += hpChange;
        if (hp>100)
        {
            hp = 100;
        }
        if (hp<0)
        {
            hp = 0;
        }
    }

    public void SetFogTexture(Texture2D fogTexture2D)//从其他脚本来改变纹理，也是用来存储纹理
    {
        fogTexture = fogTexture2D;
    }

    public void AddFlag(string flag)//从其他脚本来添加现有标志
    {
        if (flags==null)
        {
            flags.Add(flag);
        }
        else
        {
            foreach (string f in flags)//遍历标志池中是否有相同标志
            {
                if (f == flag)
                {
                    return;
                }
            }
            flags.Add(flag);
        }
    }

    public void DeleteFlag(string flag)//从其他脚本来删除现有标志
    {
        foreach (string f in flags)
        {
            if (f == flag)
            {
                flags.Remove(flag);
                return;
            }
        }
    }

    public bool HasFlag(string flag)//从其他脚本判断是否有传输的该标识
    {
        if (Flags == null)//所拥有标识为空
        {
            return false;
        }
        foreach (var flagTemp in Flags)//遍历所拥有的标识
        {
            if (flagTemp == flag)//有符合条件一模一样的
            {
                return true;
            }
        }
        return false;
    }

    public void ChangePortableSpace(int portableSpaceTemp)//从其他脚本改变可携带空间大小
    {
        portableSpace += portableSpaceTemp;
    }

    public bool CountPortableSpaceHaveBeyond()//判断是否所携带东西超出能携带空间，超过返回true，没超过返回false
    {
        needSpace = 0;
        if (PropsPoolOutside == null)
        {
            needSpace += 0;
        }
        else
        {
            foreach (var p in PropsPoolOutside)
            {
                needSpace += p.propNeedSpace*p.propCount;
            }
        }
        needSpace += FoodsToCarry;
        if (needSpace > PortableSpace)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddPropOutside(string propNameTemp,float propNeedSpaceTemp,int propCountTemp)//在背包添加道具
    {
        if (propsPoolOutside!=null)
        {
            foreach (Prop p in propsPoolOutside)
            {
                if (p.propName == propNameTemp)//如果发现新增的道具是旧道具就将道具数量加上所添加的数量
                {
                    p.propCount += propCountTemp;
                    return;
                }
            }
        }

        //以下是未发现是旧道具，从而创建新道具对象的操作
        propsPoolOutside.Add(new Prop()
        {
            propName = propNameTemp,
            propNeedSpace = propNeedSpaceTemp,
            propCount = propCountTemp
        }
        );
    }

    public void ReducePropOutside(string propNameTemp,int propCountTemp)//在背包减少道具数量
    {
        if (propsPoolOutside == null)
        {
            return;
        }
        foreach (Prop p in propsPoolOutside)
        {
            if (p.propName==propNameTemp)
            {
                if (p.propCount -propCountTemp<0)
                {
                    Debug.Log("没有足够的道具给你减");
                }
                else if(p.propCount - propCountTemp == 0)
                {
                    propsPoolOutside.Remove(p);
                }
                else
                {
                    p.propCount -= propCountTemp;
                }
                return;//无论结果如何都不用再遍历了
            }
        }
    }

    public void AddPropInHome(string propNameTemp, float propNeedSpaceTemp, int propCountTemp)//在仓库中添加道具
    {
        if (propsPoolInHome!=null)
        {
            foreach (Prop p in propsPoolInHome)
            {
                if (p.propName == propNameTemp)//如果发现新增的道具是旧道具就将道具数量加上所添加的数量
                {
                    p.propCount += propCountTemp;
                    return;
                }
            }
        }

        //以下是未发现是旧道具，从而创建新道具对象的操作
        propsPoolInHome.Add(new Prop()
        {
            propName = propNameTemp,
            propNeedSpace = propNeedSpaceTemp,
            propCount = propCountTemp
        }
        );
    }

    public void ReducePropInHome(string propNameTemp, int propCountTemp)//在仓库中减少道具数量
    {
        if (propsPoolInHome == null)
        {
            return;
        }
        foreach (Prop p in propsPoolInHome)
        {
            if (p.propName == propNameTemp)
            {
                if (p.propCount - propCountTemp < 0)
                {
                    Debug.Log("没有足够的道具给你减");
                }
                else if (p.propCount - propCountTemp == 0)
                {
                    propsPoolInHome.Remove(p);
                }
                else
                {
                    p.propCount -= propCountTemp;
                }
                return;//无论结果如何都不用再遍历了
            }
        }
    }

    public void PropsFromBackpackToWarehouse()//将背包中的道具全部塞进仓库中
    {
        if (propsPoolOutside!=null)
        {
            List<Prop> propsPoolTemp = new List<Prop>();//用来临时承载背包中的道具
            foreach (Prop p in propsPoolOutside)//遍历背包中道具
            {
                propsPoolTemp.Add(p);
            }
            propsPoolOutside.Clear();//清理背包
            foreach (Prop p in propsPoolTemp)//把临时承载的道具池中的道具放进仓库中
            {
                AddPropInHome(p.propName,p.propNeedSpace,p.propCount);
            }
            propsPoolTemp.Clear();
        }
    }

    public void ChangeCanRefreshPropsToTrue()//改变可以刷新道具管理栏的道具为true
    {
        canRefreshProps = true;
    }

    public void ChangeCanRefreshPropsToFalse()//改变可以刷新道具管理栏的道具为false
    {
        canRefreshProps = false;
    }

    public bool FindPropsInHome(string propName)//查找仓库里特定道具的方法
    {
        if (propsPoolInHome==null)
        {
            return false;
        }
        else
        {
            foreach (Prop p in propsPoolInHome)
            {
                if (p.propName==propName)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public bool FindPropsOutside(string propName)//查找背包里特定道具的方法
    {
        if (propsPoolOutside == null)
        {
            return false;
        }
        else
        {
            foreach (Prop p in propsPoolOutside)
            {
                if (p.propName == propName)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void SetCityDirection(string direction)//用于设置幸存区方位
    {
        cityDirection = direction;
    }
}
