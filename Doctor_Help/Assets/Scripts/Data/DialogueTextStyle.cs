using UnityEngine;

namespace DoctorHelp
{
    // Shared by every speaker prefab so readability is tuned once, not on each generated NPC.
    [CreateAssetMenu(menuName = "Doctor Help/Dialogue/Dialogue Text Style", fileName = ResourceName)]
    public sealed class DialogueTextStyle : ScriptableObject
    {
        internal const string ResourceName = "DialogueTextStyle";

        [Header("Office - khám bệnh")]
        [SerializeField, Range(2f, 10f), Tooltip("Chiều cao một dòng chữ khi khám bệnh, tính theo % bề rộng Safe Area. 4,2% ≈ 45 px trên màn hình rộng 1080 px.")]
        private float lineHeightPercent = 4.2f;
        [SerializeField, Range(.3f, .95f), Tooltip("Bề rộng tối đa của khung chat khi khám, so với Safe Area. Khung còn bị giới hạn bởi khoảng trống bên trái mặt bệnh nhân.")]
        private float maximumBoxWidth = .72f;
        [SerializeField, Range(0f, .5f), Tooltip("Phần bên trái của sprite Head (gồm cả tóc) mà khung chat được phép che, tính theo bề rộng đầu. Tăng nếu khung quá hẹp, giảm nếu khung che mặt.")]
        private float faceOverlap = .15f;

        [Header("Street, Player Home, phòng khám lân cận và Ghost")]
        [SerializeField, Range(2f, 10f), Tooltip("Chiều cao một dòng chữ ở các scene đi bộ, tính theo % bề rộng Safe Area.")]
        private float walkingLineHeightPercent = 4.2f;
        [SerializeField, Range(.3f, .95f), Tooltip("Bề rộng tối đa của khung chat ở các scene đi bộ, so với Safe Area.")]
        private float walkingMaximumBoxWidth = .6f;

        [Header("Khung chat")]
        [SerializeField, Min(1f), Tooltip("Bề rộng tối thiểu của vùng chữ, tính theo số lần chiều cao dòng, để câu ngắn không tạo khung quá bé.")]
        private float minimumTextWidthInLines = 3f;
        [SerializeField, Tooltip("Khoảng cách từ mép sprite ChatBox tới vùng chữ, tính bằng pixel của sprite: trái, trên, phải, dưới.")]
        private Vector4 contentInsetPixels = new Vector4(22, 20, 52, 36);
        [SerializeField, Tooltip("Đầu đuôi khung chat trong sprite, tính bằng pixel từ góc dưới trái. Điểm này được giữ cạnh người nói.")]
        private Vector2 tailTipPixels = new Vector2(240, 4);
        [SerializeField, Min(0f), Tooltip("Khoảng trống giữa chữ và nút Next, tính theo số lần chiều cao dòng.")]
        private float nextSpacingInLines = .15f;

        private static DialogueTextStyle current;

        internal static DialogueTextStyle Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<DialogueTextStyle>(ResourceName);
                if (current == null)
                {
                    current = CreateInstance<DialogueTextStyle>();
                    current.hideFlags = HideFlags.DontSave;
                }
                return current;
            }
        }

        internal float LineHeightFraction(DialogueTextSize size) =>
            (size == DialogueTextSize.Examination ? lineHeightPercent : walkingLineHeightPercent) / 100f;
        internal float MaximumBoxWidth(DialogueTextSize size) =>
            size == DialogueTextSize.Examination ? maximumBoxWidth : walkingMaximumBoxWidth;
        internal float FaceOverlap => faceOverlap;
        internal float MinimumTextWidthInLines => minimumTextWidthInLines;
        internal Vector4 ContentInsetPixels => contentInsetPixels;
        internal Vector2 TailTipPixels => tailTipPixels;
        internal float NextSpacingInLines => nextSpacingInLines;
    }
}
