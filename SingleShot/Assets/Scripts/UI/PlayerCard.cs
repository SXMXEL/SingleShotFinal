using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Image _home, _away;
        [SerializeField] private TextMeshProUGUI _username;
        [SerializeField] private TextMeshProUGUI _level;
        [SerializeField] private TextMeshProUGUI _score;
        [SerializeField] private TextMeshProUGUI _deaths;
        [SerializeField] private TextMeshProUGUI _kills;

        public void Init(
            bool TDM,
            bool away,
            string username,
            int level,
            int score,
            int kills,
            int deaths)
        {
            _username.text = username;
            _level.text = level.ToString("00");
            _score.text = score.ToString();
            _deaths.text = deaths.ToString();
            _kills.text = kills.ToString();

            if (!TDM) return;
            if (away)
            {
                _away.gameObject.SetActive(true);
            }
            else
            {
                _home.gameObject.SetActive(true);
            }
        }
    }
}