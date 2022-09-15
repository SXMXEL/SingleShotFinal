using TMPro;
using UnityEngine;

namespace UI
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _mode;
        [SerializeField] private TextMeshProUGUI _map;
        [SerializeField] private TextMeshProUGUI _homeScore;
        [SerializeField] private TextMeshProUGUI _awayScore;

        public void Init(
            bool TDM,
            string mode,
            string map,
            int homeScore,
            int awayScore)
        {
            _mode.text = mode;
            _map.text = map;

            if (TDM)
            {
                _homeScore.text = homeScore.ToString();
                _awayScore.text = awayScore.ToString();
            }
        }
    }
}
