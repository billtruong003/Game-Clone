using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// Arrow Out exit on a paper card: the arrow head slides along its grid path (accelerating), leaving an ink brush
    /// stroke (TrailRenderer + InkBrush shader) that dries out from the tail, and puffs a little smoke at the card edge.
    /// </summary>
    public sealed class InkArrowDemo : MonoBehaviour
    {
        [SerializeField] private FxSandbox sandbox;

        [Header("Card")]
        [SerializeField] private int gridSize = 6;
        [SerializeField] private float cell = 0.95f;
        [SerializeField] private Vector2 cardCenter = new(0f, 0.5f);

        [Header("Arrow")]
        [SerializeField] private Color arrowColor = new(0.306f, 0.659f, 0.871f, 1f);
        [SerializeField, Tooltip("Grid moves from the start cell: R/L/U/D letters.")] private string path = "RRUURRR";
        [SerializeField] private Vector2Int startCell = new(0, 1);
        [SerializeField] private float startSpeed = 3f, maxSpeed = 16f, acceleration = 30f;

        [Header("Ink trail")]
        [SerializeField] private float trailTime = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float trailWidth = 0.42f;   // fraction of a cell
        [SerializeField, Range(0f, 1f)] private float tailWidth = 0.2f;       // fraction of the head width
        [SerializeField] private string exitEffect = "Land_Poof";
        [SerializeField] private float exitEffectScale = 0.3f;
        [SerializeField] private float holdAfter = 0.9f;

        private bool running;
        private Material inkMaterial;

        public void Run()
        {
            if (!running) StartCoroutine(Sequence());
        }

        private Vector3 CellPos(Vector2Int c) =>
            new(cardCenter.x + (c.x - (gridSize - 1) * 0.5f) * cell, cardCenter.y + (c.y - (gridSize - 1) * 0.5f) * cell, 0f);

        private IEnumerator Sequence()
        {
            running = true;
            var root = new GameObject("InkArrowDemo").transform;
            float half = gridSize * cell * 0.5f;

            var card = SandboxArt.Sprite(root, "round_rect", UIKit.Hex("#FFF8EC"), 1f, 1);
            card.drawMode = SpriteDrawMode.Sliced;
            card.transform.localScale = Vector3.one;
            card.size = new Vector2(gridSize * cell + 0.5f, gridSize * cell + 0.5f);
            card.transform.position = cardCenter;
            for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                SandboxArt.Sprite(root, "grid_dot", UIKit.Hex("#D8D0C0"), cell * 0.18f, 2).transform.position = CellPos(new Vector2Int(x, y));

            // path: cell centers, then straight on past the card edge
            var points = new List<Vector3> { CellPos(startCell) };
            var c = startCell;
            var dir = Vector2Int.right;
            foreach (var ch in path)
            {
                dir = ch switch { 'L' => Vector2Int.left, 'U' => Vector2Int.up, 'D' => Vector2Int.down, _ => Vector2Int.right };
                c += dir;
                points.Add(CellPos(c));
            }
            var exit = points[^1] + (Vector3)(Vector2)dir * (half + cell * 3f);
            points.Add(exit);

            var head = SandboxArt.Sprite(root, "arrow_head", arrowColor, cell * 0.9f, 6);
            head.transform.position = points[0];
            var trail = head.gameObject.AddComponent<TrailRenderer>();
            if (inkMaterial == null) inkMaterial = new Material(Shader.Find("CasualGame/InkBrush"));
            trail.sharedMaterial = inkMaterial;
            trail.time = trailTime;
            trail.widthMultiplier = cell * trailWidth;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, tailWidth);
            trail.textureMode = LineTextureMode.Stretch;
            trail.minVertexDistance = 0.04f;
            trail.numCornerVertices = 3;
            trail.numCapVertices = 2;
            trail.startColor = trail.endColor = arrowColor;
            trail.sortingOrder = 4;
            trail.emitting = false;
            yield return new WaitForSeconds(0.3f);
            trail.Clear();
            trail.emitting = true;

            float speed = startSpeed;
            bool puffed = false;
            var edgeX = cardCenter.x + half;
            for (int seg = 1; seg < points.Count; seg++)
            {
                var from = points[seg - 1];
                var to = points[seg];
                var d = (to - from).normalized;
                head.transform.rotation = Quaternion.FromToRotation(Vector3.up, d); // arrow_head is drawn pointing up
                var p = from;
                while ((to - p).sqrMagnitude > 1e-6f)
                {
                    speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
                    p = Vector3.MoveTowards(p, to, speed * Time.deltaTime);
                    head.transform.position = p;
                    inkMaterial.SetFloat("_Length", Mathf.Max(0.5f, speed * trailTime));
                    if (!puffed && !string.IsNullOrEmpty(exitEffect) && p.x >= edgeX)
                    {
                        puffed = true;
                        sandbox.Play(exitEffect, new Vector3(edgeX, p.y, 0f), Color.white, exitEffectScale);
                    }
                    yield return null;
                }
            }
            head.enabled = false;
            yield return new WaitForSeconds(trailTime + holdAfter);
            Destroy(root.gameObject);
            running = false;
        }

        private void OnDestroy()
        {
            if (inkMaterial != null) Destroy(inkMaterial);
        }
    }
}
