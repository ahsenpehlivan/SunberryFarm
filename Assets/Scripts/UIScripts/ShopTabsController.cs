using UnityEngine;
using UnityEngine.UIElements;

public class ShopTabsController : MonoBehaviour
{
    [SerializeField] private UIDocument doc;

    private VisualElement page1, page2, page3, page4;
    private Button tab1, tab2, tab3, tab4;

    private void Awake()
    {
        if (!doc) doc = GetComponent<UIDocument>();

        var root = doc.rootVisualElement;

        tab1 = root.Q<Button>("Tab1");
        tab2 = root.Q<Button>("Tab2");
        tab3 = root.Q<Button>("Tab3");
        tab4 = root.Q<Button>("Tab4");

        page1 = root.Q<VisualElement>("Page1");
        page2 = root.Q<VisualElement>("Page2");
        page3 = root.Q<VisualElement>("Page3");
        page4 = root.Q<VisualElement>("Page4");

        tab1.clicked += () => ShowPage(1);
        tab2.clicked += () => ShowPage(2);
        tab3.clicked += () => ShowPage(3);
        tab4.clicked += () => ShowPage(4);

        ShowPage(1);
    }

    private void ShowPage(int index)
    {
        page1.style.display = (index == 1) ? DisplayStyle.Flex : DisplayStyle.None;
        page2.style.display = (index == 2) ? DisplayStyle.Flex : DisplayStyle.None;
        page3.style.display = (index == 3) ? DisplayStyle.Flex : DisplayStyle.None;
        page4.style.display = (index == 4) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
