using System.Collections;
using SaveScripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Objects
{
    public class Door : MonoBehaviour
    {
        public bool isOpen;
        public string nextScene = "";
        public Animator crossFade;

        private void Awake()
        {
            isOpen = false;
            var spriteChanger = GetComponent<DoorSpriteChanger>();
            if(spriteChanger != null) spriteChanger.closeDoor();
        }

        public void Update()
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemyCount <= 0)
            {
                isOpen = true;
                var spriteChanger = GetComponent<DoorSpriteChanger>();
                if(spriteChanger != null) spriteChanger.openDoor();
            }
        }

        public void OnTriggerStay2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player") && isOpen)
            {
                if (nextScene == "")
                {
                    GameStateController gameStateController = GameObject.Find("GameStateController").GetComponent<GameStateController>().GetInstance();
                    gameStateController.SaveCombatStats();
                    gameStateController.isTransition = true;
                    gameStateController.nextLevel++;
                    nextScene = gameStateController.levels[gameStateController.nextLevel];
                    //SceneManager.LoadScene(nextScene);
                    StartCoroutine(LoadNextLevel(nextScene));
                }
                else
                {
                    //SceneManager.LoadScene(nextScene);
                    StartCoroutine(LoadNextLevel(nextScene));
                }
            }
        }

        IEnumerator LoadNextLevel(string levelName)
        {
            // --- УЛУЧШЕНИЕ ЗДЕСЬ ---
            // Сначала мы проверяем, а назначили ли мы вообще аниматор в инспекторе.
            // Переменная 'crossFade' НЕ является пустой (None)?
            if (crossFade != null)
            {
                // Если аниматор ЕСТЬ, то запускаем анимацию затухания.
                crossFade.SetTrigger("Start");
            }
            // Если аниматора НЕТ, мы просто пропускаем этот блок и не ломаем игру!

            // Ждем 1 секунду. Это даст время анимации проиграться, если она была.
            yield return new WaitForSeconds(1f);

            // Теперь смело загружаем следующую сцену. Эта строка теперь выполнится всегда.
            SceneManager.LoadScene(levelName);
        }
    }
}