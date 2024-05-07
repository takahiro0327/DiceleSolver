using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Dice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    Text m_Text;

    [SerializeField]
    Image m_Background;

    [SerializeField]
    Image m_Selected;

    bool m_OnPointer;
    ColorType m_Color;

    public bool IsBig
    {
        get; set;
    } = false;

    public enum ColorType
    {
        Yellow, Green, White,
    }

    public int Number
    {
        get
        {
            return int.Parse(m_Text.text);
        }

        set
        {
            m_Text.text = value.ToString();
        }
    }

    public bool Selected
    {
        set
        {
            m_Selected.gameObject.SetActive(value);
        }
    }

    public ColorType Color
    {
        get
        {
            return m_Color;
        }

        set
        {
            m_Color = value;

            switch(m_Color)
            {
                case ColorType.Yellow:
                    m_Background.color = new Color(1, 1, 0);
                    break;

                case ColorType.Green:
                    m_Background.color = new Color(0, 1, 0);
                    break;

                case ColorType.White:
                    m_Background.color = new Color(1, 1, 1);
                    break;
            }
        }
    }

    void Awake()
    {
        Color = ColorType.White;
        Selected = false;
    }

    public void OnClick()
    {
        if( IsBig )
        {
            Number = Number + 2;
            return;
        }

        int n = Number;

        if (n == 6)
            n = 1;
        else
            ++n;

        Number = n;
    }

    void Update()
    {
        if(m_OnPointer && Input.GetMouseButtonUp(1) )
        {
            if (IsBig)
            {
                Number = Number - 1;
                return;
            }

            int v = (int)Color;
            v = (v + 1) % 3;
            Color = (ColorType)v;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_OnPointer = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_OnPointer = false;
    }
}
