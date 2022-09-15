using TMPro;
using UnityEngine;

namespace UI
{
    public class RoomButton : MonoBehaviour
    {
        public string RoomName => _roomName.text;
        [SerializeField] private TextMeshProUGUI _mapName;
        [SerializeField] private TextMeshProUGUI _roomName;
        [SerializeField] private TextMeshProUGUI _playersCount;

        public void Init(string mapName, string roomName, string playersCount)
        {
            _mapName.text = mapName;
            _roomName.text = roomName;
            _playersCount.text = playersCount;
        }
    }
}
