using UnityEngine;

namespace CasualGame.EyeBlast
{
    /// <summary>What the game is doing this frame, read by every <see cref="BlockLive"/> (world space; EyeBlastGame writes it).</summary>
    public static class BlastLive
    {
        public static bool Dragging;
        public static Vector3 Piece;            // the dragged piece's centre
        public static Vector2 PieceVelocity;    // cells per second
        public static Color PieceColor = Color.white;
        public static Vector3 BoardCenter;
        public static float BoardBottom = 0.27f, BoardHeight = 0.47f; // on screen, 0..1 of the screen height
        public static float Cell = 1f;          // one board cell, world units
        public static readonly float[] RowFill = new float[BlastBoard.Size];
        public static float PlaceTime = -9f;
        public static Vector3 PlaceAt;
    }

    public enum BlockLiveKind { Watch, City, Chrome, Aquarium }

    /// <summary>
    /// Drives one block of an interactive premium set (Assets/_Game/Skins/Blocks): Watchers look at the dragged piece,
    /// Night City lights up as its row fills, Chrome reflects the piece, Aquarium sloshes when dragged or bumped.
    /// </summary>
    public sealed class BlockLive : MonoBehaviour
    {
        public BlockLiveKind Kind;
        public Material Mat;
        public int Row = -1;          // board row, -1 for a block of a tray / dragged piece
        public bool InPiece;

        private float clearStart = -1f, blinkOffset, lit, tilt, tiltVel, cityScale = -1f, cityHorizon;
        private Vector2 look;

        private void Start() => blinkOffset = Random.value * 4f;

        /// <summary>The block's line is clearing: play the skin's clear (lights out, eyes shut, water drains).</summary>
        public void Clear(float delay) => clearStart = Time.time + delay;

        private void OnDestroy()
        {
            if (Mat != null) Destroy(Mat);
        }

        private void Update()
        {
            if (Mat == null) return;
            var dt = Time.deltaTime;
            var pos = transform.position;
            var cell = Mathf.Max(BlastLive.Cell, 1e-4f);
            var clear = clearStart >= 0f ? Mathf.Clamp01((Time.time - clearStart) / 0.25f) : 0f;
            var toPiece = (Vector2)(BlastLive.Piece - pos) / cell;
            var near = BlastLive.Dragging && !InPiece ? Mathf.InverseLerp(3f, 0.9f, toPiece.magnitude) : 0f;
            switch (Kind)
            {
                case BlockLiveKind.Watch:
                    var want = InPiece ? Vector2.down * 0.6f : BlastLive.Dragging ? Vector2.ClampMagnitude(toPiece / 2.5f, 1f) : look * 0.98f;
                    look = Vector2.Lerp(look, want, dt * 10f);
                    Mat.SetVector("_Look", look);
                    Mat.SetFloat("_Alarm", near);
                    var b = Mathf.Repeat(Time.time + blinkOffset, 4.1f);
                    Mat.SetFloat("_Blink", Mathf.Max(clear > 0f ? 1f : 0f, b < 0.14f ? Mathf.Sin(b / 0.14f * Mathf.PI) : 0f));
                    break;
                case BlockLiveKind.City:
                    // the city stands on the board's bottom edge and fills it, wherever the game puts the board
                    // (the template's values are the lab's: board bottom 0.27, height 0.47 of the screen)
                    if (cityScale < 0f) { cityScale = Mat.GetFloat("_CityScale"); cityHorizon = Mat.GetFloat("_Horizon") - 0.27f; }
                    Mat.SetFloat("_Horizon", BlastLive.BoardBottom + cityHorizon);
                    Mat.SetFloat("_CityScale", cityScale * 0.47f / Mathf.Max(BlastLive.BoardHeight, 0.05f));
                    var target = Row >= 0 ? Mathf.Pow(BlastLive.RowFill[Row], 1.6f) : 0.55f;
                    lit = Mathf.MoveTowards(lit, target, dt * 1.5f);
                    Mat.SetFloat("_Lit", lit);
                    Mat.SetFloat("_Clear", clear);
                    var par = BlastLive.Dragging ? (Vector2)(BlastLive.Piece - BlastLive.BoardCenter) / (cell * 4f) : Vector2.zero;
                    Mat.SetVector("_Parallax", Vector2.ClampMagnitude(par, 1f));
                    break;
                case BlockLiveKind.Chrome:
                    Mat.SetVector("_Reflect", BlastLive.Dragging ? Vector2.ClampMagnitude(toPiece / 4.5f, 1f) : Vector2.zero);
                    Mat.SetFloat("_Ghost", near);
                    Mat.SetColor("_GhostColor", BlastLive.PieceColor);
                    break;
                default:
                    var tiltWant = InPiece && BlastLive.Dragging ? Mathf.Clamp(-BlastLive.PieceVelocity.x / 6f, -1f, 1f) : 0f;
                    if (!InPiece && Time.time - BlastLive.PlaceTime < dt * 1.5f && Vector2.Distance(BlastLive.PlaceAt, pos) / cell < 2.7f) tiltVel += 6f;
                    tiltVel += ((tiltWant - tilt) * 60f - tiltVel * 5f) * dt;
                    tilt += tiltVel * dt;
                    Mat.SetFloat("_Tilt", Mathf.Clamp(tilt, -1f, 1f));
                    Mat.SetFloat("_Slosh", Mathf.Clamp01(Mathf.Abs(tiltVel) * 0.2f));
                    Mat.SetFloat("_Drain", clear);
                    break;
            }
        }
    }
}
