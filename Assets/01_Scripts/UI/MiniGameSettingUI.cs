
using UnityEngine;
using UnityEngine.UI;

public class MiniGameSettingUI : MonoBehaviour
{
    [Header("<< Mini Game Toggles >>")]
    [SerializeField] private Toggle baseballToggle;
    [SerializeField] private Toggle colorBlockToggle;
    [SerializeField] private Toggle dodgeItToggle;
    [SerializeField] private Toggle whackAMoleToggle;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "[MiniGameSettingUI] GameManager를 찾을 수 없습니다."
            );
            return;
        }

        // 현재 설정을 토글 UI에 반영
        baseballToggle.SetIsOnWithoutNotify(
            gameManager.IsMiniGameEnabled("02_Baseball"));

        colorBlockToggle.SetIsOnWithoutNotify(
            gameManager.IsMiniGameEnabled("02_ColorBlock"));

        dodgeItToggle.SetIsOnWithoutNotify(
            gameManager.IsMiniGameEnabled("02_DodgeIt"));

        whackAMoleToggle.SetIsOnWithoutNotify(
            gameManager.IsMiniGameEnabled("02_WhackaMole"));

        // 토글 변경 이벤트 등록
        baseballToggle.onValueChanged.AddListener(
            value => SetMiniGame("02_Baseball", value));

        colorBlockToggle.onValueChanged.AddListener(
            value => SetMiniGame("02_ColorBlock", value));

        dodgeItToggle.onValueChanged.AddListener(
            value => SetMiniGame("02_DodgeIt", value));

        whackAMoleToggle.onValueChanged.AddListener(
            value => SetMiniGame("02_WhackaMole", value));
    }

    private void SetMiniGame(string sceneName, bool isEnabled)
    {
        if (gameManager == null)
            return;

        gameManager.SetMiniGameEnabled(sceneName, isEnabled);
    }

    private void OnDestroy()
    {
        if (baseballToggle != null)
            baseballToggle.onValueChanged.RemoveAllListeners();

        if (colorBlockToggle != null)
            colorBlockToggle.onValueChanged.RemoveAllListeners();

        if (dodgeItToggle != null)
            dodgeItToggle.onValueChanged.RemoveAllListeners();

        if (whackAMoleToggle != null)
            whackAMoleToggle.onValueChanged.RemoveAllListeners();
    }
}
