using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 통일된 디자인 시스템 — 색상, 타이포, 스프라이트, 헬퍼 메서드
    /// 모든 UI는 이 테마를 통해 생성/스타일링
    /// </summary>
    public static class UITheme
    {
        // ─── 색상 팔레트 (다크 테마) ──────────────────────────────────
        public static readonly Color BgOverlay      = new(0f,    0f,    0f,    0.65f);    // 모달 배경
        public static readonly Color Surface        = new(0.08f, 0.08f, 0.10f, 0.97f);    // 패널
        public static readonly Color SurfaceRaised  = new(0.12f, 0.12f, 0.14f, 1f);       // 카드
        public static readonly Color SurfaceSubtle  = new(0.16f, 0.16f, 0.19f, 1f);       // 입력/슬롯
        public static readonly Color Border         = new(1f,    1f,    1f,    0.08f);    // 구분선

        public static readonly Color TextPrimary    = new(0.96f, 0.96f, 0.98f, 1f);
        public static readonly Color TextSecondary  = new(0.65f, 0.65f, 0.70f, 1f);
        public static readonly Color TextMuted      = new(0.40f, 0.40f, 0.44f, 1f);

        public static readonly Color Accent         = new(0f,    1f,    0.53f, 1f);       // 에메랄드 #00ff88
        public static readonly Color AccentSoft     = new(0f,    1f,    0.53f, 0.15f);
        public static readonly Color Gold           = new(1f,    0.84f, 0f,    1f);       // 베테랑/궁극기
        public static readonly Color Danger         = new(1f,    0.30f, 0.35f, 1f);       // 빨간 액션
        public static readonly Color DangerSoft     = new(0.55f, 0.10f, 0.15f, 1f);
        public static readonly Color Info           = new(0.20f, 0.55f, 1f,    1f);       // 파란 액션
        public static readonly Color Purple         = new(0.55f, 0.30f, 0.95f, 1f);       // 합성/특수

        // ─── 폰트 사이즈 ───────────────────────────────────────────────
        public const float FontDisplay = 28f;  // 타이틀
        public const float FontHeader  = 20f;  // 패널 헤더
        public const float FontTitle   = 16f;  // 카드 타이틀
        public const float FontBody    = 14f;  // 본문
        public const float FontCaption = 12f;  // 라벨
        public const float FontMicro   = 10f;  // 배지

        // ─── 모서리 / 간격 ────────────────────────────────────────────
        public const float RadiusSm = 6f;
        public const float RadiusMd = 12f;
        public const float RadiusLg = 18f;

        // ─── 스프라이트 캐시 ──────────────────────────────────────────
        static Sprite _roundedSprite;
        static Sprite _circleSprite;

        /// <summary>둥근 사각형 9-slice 스프라이트 (반지름 16px)</summary>
        public static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;

            const int size   = 64;
            const int radius = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsRoundedRectPixel(x, y, size, size, radius);
                    pixels[y * size + x] = inside ? Color.white : new Color(1, 1, 1, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // 9-slice border = 반지름 (가운데는 늘어나도 됨)
            _roundedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            _roundedSprite.name = "UITheme_Rounded";
            return _roundedSprite;
        }

        /// <summary>완전한 원형 스프라이트 (아바타, 점멸 등)</summary>
        public static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float aa = Mathf.Clamp01(r - d);
                    pixels[y * size + x] = new Color(1, 1, 1, aa);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _circleSprite.name = "UITheme_Circle";
            return _circleSprite;
        }

        static bool IsRoundedRectPixel(int x, int y, int w, int h, int r)
        {
            // 안티앨리어싱 없이 단순 in/out 판정 (9-slice라 모서리만 표시됨)
            int ix = x < r ? r - x : (x >= w - r ? x - (w - r) + 1 : 0);
            int iy = y < r ? r - y : (y >= h - r ? y - (h - r) + 1 : 0);
            if (ix == 0 || iy == 0) return true;
            return ix * ix + iy * iy <= r * r;
        }

        // ─── 헬퍼: Image에 둥근 배경 적용 ─────────────────────────────
        public static Image ApplyRoundedBg(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            return img;
        }

        // ─── 헬퍼: 카드에 그림자 효과 ─────────────────────────────────
        public static Shadow AddShadow(GameObject go, float distance = 4f, float alpha = 0.5f)
        {
            var sh = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, alpha);
            sh.effectDistance = new Vector2(distance, -distance);
            return sh;
        }

        // ─── 헬퍼: TMP 라벨 스타일링 ──────────────────────────────────
        public static void StyleHeader(TextMeshProUGUI tmp)
        {
            tmp.fontSize = FontHeader;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = TextPrimary;
        }
        public static void StyleBody(TextMeshProUGUI tmp)
        {
            tmp.fontSize = FontBody;
            tmp.color = TextPrimary;
        }
        public static void StyleCaption(TextMeshProUGUI tmp)
        {
            tmp.fontSize = FontCaption;
            tmp.color = TextSecondary;
        }
    }
}
