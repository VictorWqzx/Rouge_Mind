// DialogueUI.cs
using UnityEngine;
using TMPro; // Важно для работы с TextMeshPro
using UnityEngine.UI; // Важно для работы с Button

public class DialogueUI : MonoBehaviour
{
    [Header("Компоненты UI")]
    [SerializeField] private GameObject dialoguePanelObject; // Ссылка на саму панель
    [SerializeField] private TextMeshProUGUI speakerNameText;   // Текст для имени говорящего
    [SerializeField] private TextMeshProUGUI messageText;       // Текст для основной реплики
    [SerializeField] public TMP_InputField replyInputField;      // Поле ввода для ответа игрока
    [SerializeField] private Button sendButton;              // Кнопка "Ответить"

    // Ссылка на контроллер Друида, которую мы найдем автоматически
    private DruidAI druidAI;

    // Замени свой старый метод Start в DialogueUI.cs на этот

    private void Start()
    {
        // Ищем друида, как и раньше
        druidAI = FindObjectOfType<DruidAI>();

        // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
        // Добавляем проверку: а точно ли у нас есть кнопка?
        // Это делает код более устойчивым к ошибкам загрузки.
        if (sendButton != null)
        {
            // Назначаем действие только если кнопка существует.
            sendButton.onClick.AddListener(OnSendButtonClick);
        }
        else
        {
            Debug.LogError("Кнопка 'Send Button' не назначена в инспекторе для DialogueUI!");
        }

        // Проверяем и панель тоже на всякий случай
        if (dialoguePanelObject != null)
        {
            dialoguePanelObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Панель 'Dialogue Panel Object' не назначена в инспекторе для DialogueUI!");
        }
    }

    // Этот метод будет вызываться по клику на кнопку "Ответить".
    private void OnSendButtonClick()
    {
        // Получаем текст, который ввел игрок
        string playerReply = replyInputField.text;

        // Проверяем, что текст не пустой и что мы нашли друида
        if (!string.IsNullOrWhiteSpace(playerReply) && druidAI != null)
        {
            // Вызываем публичный метод из скрипта друида и передаем ему наш ответ
            druidAI.SendPlayerReply(playerReply);

            // Очищаем поле ввода для следующей реплики
            replyInputField.text = "";

            // Снова делаем поле ввода активным, чтобы игрок мог сразу печатать дальше
            replyInputField.Select();
            replyInputField.ActivateInputField();
        }
    }

    // Публичный метод, чтобы друид мог управлять видимостью панели
    public void ShowDialogue(bool show)
    {
        dialoguePanelObject.SetActive(show);
    }

    // Публичный метод, чтобы друид мог выводить свои реплики в наш UI
    public void SetMessage(string speaker, string message)
    {
        speakerNameText.text = speaker;
        messageText.text = message;
    }
}