using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TypeWriter : MonoBehaviour
{
    [SerializeField] private Text diaryText;//用于打字的Text组件

    [SerializeField] private float intervalTime =0.05f;//每个字间隔时间


    private string tempDiary;//用于临时存储Text中的文本

    private bool NextStep =false;//是否进行下一步

    private string preFace = "       203？年？月？日    星期？    阴\r\n       不记得多久前了，世界被一场瘟疫毁灭了，得病的人，或许已经不能称他们为人了，像疯了似的攻击正常人，现在估计已经没有多少活人了。\r\n        我和一些幸存者建了个临时据点，来躲避它们的攻击，但是这里也不是什么不受侵犯的圣地，病的病死，饿的饿死，也有出去探索再也没能回来的。在亲手送走了病危的阿列克谢之后，这里也只有我一个人了。\r\n\r\n或许我也应该出发了，找一个新的安居点或者好归宿？\"";

    private string afterFineSurvival = "       203？年？月？日    星期？    晴\r\n       今天干掉几只行尸后，从地下室里救出了一个小姑娘，大概十三四岁的样子，她对于外面的世界发生了什么可以说是什么都不知道了，地下室里有生活的气息，还有快要耗尽的粮食，如果我没有及时发现她的话估计她就会被饿死在这儿了。\r\n       据她所说，她的父母把她锁在了这儿，并交代过没有听到他们的声音就不要开门。期间她也试图出去，但是地下室的门锁在外面，她用尽力气也推不开，这也只是次要的，在一次试图破门的时候，从门的那一边传来了剧烈的敲击声，”爸爸妈妈？“，她试探问道，回应的只有低声嘶吼，她再也没有尝试靠近那扇门，直到我砸开那扇门。\r\n       说完后我看了看被我干掉并拖进角落的那几只行尸----一男一女...\r\n       “你爸爸妈妈我遇到一次，他们现在没有办法来帮你只好委托我来救你，你就先跟我来过一阵子吧，后面我带你去找你爸妈。”";


    void Start()
    {
        if (GameController.Instance.HasFlag("双人日记"))
        {
            tempDiary = afterFineSurvival;
            GameController.Instance.DeleteFlag("双人日记");
        }
        else
        {
            tempDiary = preFace;
        }
        diaryText.text = "";//将文本初始化，用于后面从零到有的打字
        StartCoroutine(Typing());//开启打字协程，在此之前tempDiary必须赋值和diaryText必须为空
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))//点击鼠标左键即跳过打字阶段
        {
            NextStep = true;
            if (diaryText.text==tempDiary)
            {
                SceneManager.LoadScene(2);
            }
        }
    }
    IEnumerator Typing()//打字协程
    {
        foreach (char word in tempDiary.ToCharArray())//遍历临时存储文本中的字符
        {
            if (NextStep)//是否进行下一步跳过打字阶段
            {
                break;//跳出打字阶段
            }
            diaryText.text += word;//将字符累加到Text中的文本中
            yield return new WaitForSeconds(intervalTime);//加完一个字符后等待时间intervalTime
        }
        diaryText.text = tempDiary;//将文本全部打印出来
        NextStep = false;//初始化
    }
}
