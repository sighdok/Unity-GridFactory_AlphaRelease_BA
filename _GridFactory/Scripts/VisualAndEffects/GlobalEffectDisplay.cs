using UnityEngine;
using UnityEngine.UI;

using MoreMountains.Feedbacks;

namespace GridFactory.Core
{
    // TODO: TMP_Text statt Text
    public class GlobalEffectDisplay : MonoBehaviour
    {
        [SerializeField] private float display_speed = 2f;
        [SerializeField] private Text title;
        [SerializeField] private Text desc;
        [SerializeField] private Image image;
        [SerializeField] private MMF_Player effect;
        [SerializeField] private AudioClip show_audio;

        private CanvasGroup _canvas_group;
        private Animator _animator;
        private float _timer = 0f;
        private bool _visible = false;

        private void Start()
        {
            _animator = GetComponentInChildren<Animator>();
            _canvas_group = GetComponent<CanvasGroup>();
            _canvas_group.alpha = 0f;
        }

        private void Update()
        {
            float add = _visible ? display_speed : -display_speed;
            float alpha = Mathf.Clamp01(_canvas_group.alpha + add * Time.deltaTime);
            _canvas_group.alpha = alpha;

            if (!_visible && alpha < 0.01f)
                gameObject.SetActive(false);

            if (_visible)
            {
                _timer += Time.deltaTime;
                if (_timer > 4f)
                    Hide();
            }
        }

        public void ShowBox(string tit, string des, Sprite img = null, bool playEffect = true)
        {
            title.text = tit;
            desc.text = des;

            if (image != null)
            {
                image.sprite = img;
                image.enabled = img != null;
            }

            _timer = 0f;
            Show();

            if (_animator != null)
                _animator.Rebind();

            if (playEffect)
                effect.PlayFeedbacks();
        }

        public virtual void Show(bool instant = false)
        {
            _visible = true;
            gameObject.SetActive(true);

            if (instant || display_speed < 0.01f)
                _canvas_group.alpha = 1f;
        }

        public virtual void Hide(bool instant = false)
        {
            _visible = false;
            if (instant || display_speed < 0.01f)
                _canvas_group.alpha = 0f;
        }
    }
}
