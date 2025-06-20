// ГОТОВЫЙ КОД ДЛЯ DRUIDAI.CS (с триггером от игрока)

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Player;

// Классы-структуры без изменений
[System.Serializable] public class ChatMessage { public string role; public string content; }
[System.Serializable] public class ApiRequest { public string model = "mistralai/mistral-7b-instruct"; public List<ChatMessage> messages = new List<ChatMessage>(); public float temperature = 0.7f; public int max_tokens = 150; }
[System.Serializable] public class ApiResponse { public List<ApiChoice> choices; }
[System.Serializable] public class ApiChoice { public ChatMessage message; }

[RequireComponent(typeof(CircleCollider2D))]
public class DruidAI : MonoBehaviour
{
    // --- НАСТРОЙКИ ---
    [Header("API Settings")]
    [SerializeField] private string apiKey = "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";
    private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

    // ##########################################################################
    // ###          ПРОМПТ ОБНОВЛЕН С НОВЫМ ПРАВИЛОМ О ГОТОВНОСТИ            ###
    // ##########################################################################
    [Header("Druid's Personality")]
    [TextArea(15, 30)]
    [SerializeField]
    private string systemPrompt = @"Ты — Элдрин, мудрый и древний дух-хранитель этого леса, действующий как проводник. Твой тон спокоен, терпелив и полон грусти о больном лесе. Ты не экзаменатор, а наставник.

### Твои Правила:
1.  **СТИЛЬ РЕЧИ:** Говори метафорами о природе. Задавай наводящие вопросы.
2.  **ЦЕЛЬ:** Подготовить игрока к опасностям. Охотно отвечай на вопросы о лесе и его обитателях (слизь, скелеты, черные духи).
3.  **ИСТОРИЯ МИРА:** Лес болен из-за ""Тени"". Монстры — симптомы порчи.
4.  **БОЙ КАК УРОК:** Бой — это ритуал или практический урок, который ты инициируешь, когда слов недостаточно.
5.  **НОВОЕ ПРАВИЛО - ТРИГГЕР ОТ ИГРОКА:** Если игрок сам выражает готовность сражаться (фразами ""я готов"", ""сражусь"", ""начнем"", ""пойду"", ""я готов к испытанию""), ты должен воспринять это как сигнал. В ответ произнеси короткую фразу, признающую его решимость, и НЕМЕДЛЕННО начни бой-урок с помощью тега [FIGHT]. Пример: 'Твоя решимость похвальна. Что ж, пусть духи леса оценят ее. [FIGHT] Прими свой урок!'
6.  **СИГНАЛ БОЯ:** Для начала боя, либо по твоему решению, либо по просьбе игрока, твоя последняя фраза ДОЛЖНА заканчиваться тегом [FIGHT].
7.  **ТЫ СОЮЗНИК:** Ты хочешь, чтобы игрок выжил, а не просто прошел мимо.";

    [Header("Game Logic")]
    [SerializeField] private int forceFightAfterTurns = 5;
    [SerializeField] private string[] defaultMonsters = { "эхом этого леса - скелетами", "слизью, порожденной тенью" };
    [SerializeField] private KeyCode interactionKey = KeyCode.Tab;

    // #######################################################################
    // ###          НОВЫЙ СПИСОК КЛЮЧЕВЫХ СЛОВ (РЕДАКТИРУЕТСЯ В ИНСПЕКТОРЕ)  ###
    // #######################################################################
    [Header("Player Triggers")]
    [Tooltip("Слова, которые игрок может сказать, чтобы сразу начать бой. Регистр не важен.")]
    [SerializeField]
    private List<string> readyKeywords = new List<string> { "готов", "сражаться", "сражусь", "пойду", "начнем", "испытанию" };


    [Header("Dialogue Flow")]
    [SerializeField]
    private List<string> cannedDruidResponses = new List<string>
    {
        "Лишь немногие находят эту тропу. Что привело тебя в эти больные земли, дитя?",
        "Спрашивай. Возможно, у старых корней найдутся ответы для тебя. Но помни, знание здесь имеет свою цену."
    };

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;

    // Внутреннее состояние (без изменений)
    private List<ChatMessage> dialogueHistory = new List<ChatMessage>();
    private PlayerController playerController;
    private bool playerInRange = false;
    private bool dialogueActive = false;
    private int dialogueTurnCounter = 0;

    // Awake, OnTrigger и Update (без изменений)
    private void Awake() { GetComponent<CircleCollider2D>().isTrigger = true; }
    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) { playerInRange = true; playerController = other.GetComponent<PlayerController>(); } }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) { playerInRange = false; if (dialogueActive) EndDialogue(false); } }
    private void Update() { if (playerInRange && Input.GetKeyDown(interactionKey) && !dialogueActive) { StartDialogue(); } }

    private void StartDialogue()
    {
        dialogueActive = true;
        if (playerController != null) playerController.EnterDialogueMode();
        dialogueTurnCounter = 0;
        dialogueHistory.Clear();
        dialogueHistory.Add(new ChatMessage { role = "system", content = systemPrompt });
        string firstPhrase = "Тише... Ты разбудишь древнюю печаль этого леса. Зачем ты здесь?";
        dialogueHistory.Add(new ChatMessage { role = "assistant", content = firstPhrase });
        dialogueUI.ShowDialogue(true);
        dialogueUI.SetMessage("Элдрин", firstPhrase);
        dialogueUI.replyInputField.Select();
        dialogueUI.replyInputField.ActivateInputField();
    }

    // #######################################################################
    // ###          ГЛАВНОЕ ИЗМЕНЕНИЕ: ДОБАВЛЕН "СЛУШАТЕЛЬ" КЛЮЧЕВЫХ СЛОВ   ###
    // #######################################################################
    public void SendPlayerReply(string playerInput)
    {
        if (!dialogueActive) return;

        dialogueHistory.Add(new ChatMessage { role = "user", content = playerInput });
        dialogueUI.SetMessage("Путник", playerInput);

        // --- НОВАЯ ЛОГИКА - "СЛУШАТЕЛЬ" ---
        // Переводим ответ игрока в нижний регистр для удобного поиска
        string lowerInput = playerInput.ToLower();
        // Проверяем, содержит ли ответ игрока одно из ключевых слов
        foreach (string keyword in readyKeywords)
        {
            if (lowerInput.Contains(keyword))
            {
                Debug.Log("Игрок выразил готовность через ключевое слово! Форсируем бой.");
                // Немедленно вызываем бой, минуя ИИ и заготовленные ответы
                ForceStartBattleWithAcknowledgement();
                return; // Выходим из метода, чтобы не продолжать диалог
            }
        }

        // Если ключевых слов не найдено, продолжаем обычный диалог
        if (dialogueTurnCounter < cannedDruidResponses.Count)
        {
            string cannedResponse = cannedDruidResponses[dialogueTurnCounter];
            StartCoroutine(ShowCannedResponseAfterDelay(cannedResponse));
            dialogueTurnCounter++;
        }
        else
        {
            Debug.Log("Заготовленные ответы закончились. Передаю управление ИИ.");
            StartCoroutine(SendRequestToAI());
        }
    }

    // Этот метод не изменился, но теперь будет вызываться реже
    private IEnumerator ShowCannedResponseAfterDelay(string response)
    {
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
        dialogueUI.SetMessage("Элдрин", response);
        dialogueHistory.Add(new ChatMessage { role = "assistant", content = response });
        dialogueUI.replyInputField.Select();
        dialogueUI.replyInputField.ActivateInputField();
    }

    // Метод вызова ИИ не изменился
    private IEnumerator SendRequestToAI()
    {
        // ... код вызова ИИ полностью сохранен ...
        ApiRequest requestData = new ApiRequest { messages = dialogueHistory };
        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) { ForceStartBattle(); }
            else
            {
                ApiResponse responseData = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                string assistantReply = responseData.choices[0].message.content.Trim();
                dialogueHistory.Add(new ChatMessage { role = "assistant", content = assistantReply });
                dialogueUI.SetMessage("Элдрин", assistantReply);
                if (assistantReply.Contains("[FIGHT]")) { Invoke(nameof(EndDialogueAndStartBattle), 2f); }
                else { dialogueUI.replyInputField.Select(); dialogueUI.replyInputField.ActivateInputField(); }
            }
        }
    }

    // ##########################################################################
    // ###          НОВЫЙ МЕТОД ДЛЯ КОРРЕКТНОГО ОТВЕТА НА КЛЮЧЕВОЕ СЛОВО     ###
    // ##########################################################################
    private void ForceStartBattleWithAcknowledgement()
    {
        // Специальная фраза-ответ на решимость игрока
        string finalPhrase = "Твоя решимость похвальна. Приступим.";
        dialogueUI.SetMessage("Элдрин", finalPhrase);
        Invoke(nameof(EndDialogueAndStartBattle), 2f); // Начинаем бой после небольшой паузы
    }

    // Остальные вспомогательные функции без изменений
    private void EndDialogueAndStartBattle() { EndDialogue(true); }
    private void ForceStartBattle()
    {
        string monster = defaultMonsters[Random.Range(0, defaultMonsters.Length)];
        string finalPhrase = $"Довольно слов! Твое испытание - {monster}.";
        dialogueUI.SetMessage("Элдрин", finalPhrase);
        Invoke(nameof(EndDialogueAndStartBattle), 2f);
    }
    private void EndDialogue(bool startBattle)
    {
        dialogueActive = false;
        dialogueUI.ShowDialogue(false);
        if (playerController != null) playerController.ExitDialogueMode();
        if (startBattle) { Debug.LogWarning("BATTLE HAS STARTED!"); }
    }
}