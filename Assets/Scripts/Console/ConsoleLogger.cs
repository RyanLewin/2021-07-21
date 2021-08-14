using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;

public class ConsoleLogger : NetworkBehaviour
{
    public static ConsoleLogger Instance;
    public GameObject openChat;
    [SerializeField] GameObject closedChat;
    [SerializeField] GameObject consoleParent;
    [SerializeField] TextMeshProUGUI consoleMessage;
    [SerializeField] TextMeshProUGUI chatHistory;
    public TMP_InputField inputField;
    [SerializeField] List<Message> messages = new List<Message>();
    [SerializeField] float lineHeight = 50f;
    [SerializeField] float heightOffset = 30f;
    [SerializeField] float scrollAmount = 50;

    private void Awake() 
    {
        Instance = this;
        SetShowFullChat(false);
    }

    public void SetShowFullChat(bool value)
    {
        StopCoroutine("ScrollForce");
        openChat.SetActive(value);
        closedChat.SetActive(!value);
        ResetScroll();
    }

    public void ScrollText(int dir)
    {
        StopCoroutine("ScrollForce");
        StartCoroutine(ScrollForce(dir));
        // var yPos = chatHistory.rectTransform.anchoredPosition;
        // yPos.y += dir * scrollAmount * Time.deltaTime;
        // chatHistory.rectTransform.anchoredPosition = yPos;
    }

    IEnumerator ScrollForce(int dir)
    {
        float amount = scrollAmount;
        while(amount > 0)
        {
            var yPos = chatHistory.rectTransform.anchoredPosition;
            yPos.y = Mathf.Clamp(yPos.y - (dir * amount * Time.deltaTime), -chatHistory.rectTransform.sizeDelta.y + lineHeight * 2, 0);
            amount -= 5;
            chatHistory.rectTransform.anchoredPosition = yPos;
            yield return new WaitForFixedUpdate();
        }
    }

    public void ResetScroll()
    {
        chatHistory.rectTransform.anchoredPosition = Vector2.zero;
    }

    public void AddMessage(string message)
    {
        var xPos = consoleParent.GetComponent<RectTransform>().sizeDelta.x / 2;
        var newMessage = Instantiate(consoleMessage, new Vector3(xPos, heightOffset, 0), Quaternion.identity, consoleParent.transform);
        newMessage.rectTransform.anchoredPosition = new Vector2(0,0);

        newMessage.text = message;
        count = 0;
        count = Overflow(newMessage);

        foreach(var oldMessage in messages)
        {
            var pos = oldMessage.message.rectTransform.anchoredPosition;
            pos.y += count * lineHeight;
            oldMessage.message.rectTransform.anchoredPosition = pos;
        }
        messages.Add(new Message(newMessage, count));
        StartCoroutine(FadeMessage(newMessage));

        chatHistory.text += message + "\n";
        var size = chatHistory.rectTransform.sizeDelta;
        size.y += count * lineHeight;
        chatHistory.rectTransform.sizeDelta = size;
    }

    IEnumerator FadeMessage(TextMeshProUGUI newMessage)
    {
        yield return new WaitForSeconds(3f);
        var alphaZeroColour = newMessage.color;
        alphaZeroColour.a = 0;
        LeanTween.colorText(newMessage.rectTransform, alphaZeroColour, 2f).setOnComplete(() => { 
            messages.RemoveAt(0);
            Destroy(newMessage.gameObject); 
        });
    }

    int count;
    public int Overflow(TextMeshProUGUI newMessage)
    {
        count++;
        if (count > 20) 
        {
            return count;
        }

        newMessage.rectTransform.sizeDelta += new Vector2(0, lineHeight);
        newMessage.ForceMeshUpdate();
        if (newMessage.isTextOverflowing)
            return Overflow(newMessage);
        else
        {
            newMessage.rectTransform.sizeDelta -= new Vector2(0, lineHeight);
            return count;
        }
    }
}

public struct Message
{
    public TextMeshProUGUI message;
    public int lineCount;

    public Message(TextMeshProUGUI message, int lineCount)
    {
        this.message = message;
        this.lineCount = lineCount;
    }
}
