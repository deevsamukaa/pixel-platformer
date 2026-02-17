using System.Collections.Generic;
using UnityEngine;

public class CameraBoundsManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private RuntimeCameraBoundsBuilder boundsBuilder;

    [Header("Debug")]
    [SerializeField] private bool logChanges;

    private readonly List<SegmentSpec> _activeSegments = new();

    private SegmentSpec _current;

    // Casual/Full: lock permanente no End
    private SegmentSpec _locked;

    // Infinity: End hold temporário (antes do Continue)
    private SegmentSpec _infinityEndHold;

    // ✅ Novo: Ends já “liberados” (após Continue)
    private readonly HashSet<SegmentSpec> _releasedInfinityEnds = new();

    // cache aplicado
    private SegmentSpec _appliedPrev;
    private SegmentSpec _appliedCur;
    private SegmentSpec _appliedNext;

    private void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (boundsBuilder == null)
            boundsBuilder = FindAnyObjectByType<RuntimeCameraBoundsBuilder>();

        if (boundsBuilder == null)
            Debug.LogError("[CameraBoundsManager] RuntimeCameraBoundsBuilder não encontrado.");
    }

    private void OnEnable()
    {
        if (RunManager.I != null)
        {
            RunManager.I.OnCheckpointReached += HandleCheckpointReached;
            RunManager.I.OnStageChanged += HandleStageChanged;
        }
    }

    private void OnDisable()
    {
        if (RunManager.I != null)
        {
            RunManager.I.OnCheckpointReached -= HandleCheckpointReached;
            RunManager.I.OnStageChanged -= HandleStageChanged;
        }
    }

    private void Update()
    {
        if (player == null || boundsBuilder == null) return;

        bool isInfinity = IsInfinityMode();

        // 1) lock duro (Casual/Full)
        if (_locked != null)
        {
            var prevOfLocked = FindPrevSegment(_locked);
            ApplyBounds(prevOfLocked, _locked, null);
            return;
        }

        // 2) segment atual
        var cur = FindSegmentContainingPlayer();
        if (cur == null) return;
        _current = cur;

        // 3) Se é End
        if (cur.tag == SegmentTag.End)
        {
            if (!isInfinity)
            {
                // Casual/Full: End fecha sempre
                _locked = cur;
                var prev = FindPrevSegment(cur);
                ApplyBounds(prev, _locked, null);
                return;
            }

            // ✅ Infinity:
            // - Se este End já foi liberado, ele se comporta como normal (Prev+Cur+Next)
            // - Se não foi liberado, segura Prev+End (sem Next)
            if (_releasedInfinityEnds.Contains(cur))
            {
                // comportamento normal
                var prev = FindPrevSegment(cur);
                var next = FindNextSegment(cur);
                ApplyBounds(prev, cur, next);
                return;
            }

            // End ainda não liberado => HOLD
            _infinityEndHold = cur;

            var prevHold = FindPrevSegment(_infinityEndHold);
            ApplyBounds(prevHold, _infinityEndHold, null);
            return;
        }

        // 4) Se saiu do End, limpa hold (por segurança)
        if (_infinityEndHold != null)
            _infinityEndHold = null;

        // 5) Normal: Prev + Cur + Next
        var prevNormal = FindPrevSegment(cur);
        var nextNormal = FindNextSegment(cur);
        ApplyBounds(prevNormal, cur, nextNormal);
    }

    private bool IsInfinityMode()
    {
        return RunManager.I != null &&
               RunManager.I.CurrentMode != null &&
               RunManager.I.CurrentMode.type == GameModeType.Infinity;
    }

    private SegmentSpec FindSegmentContainingPlayer()
    {
        Vector2 p = player.position;

        SegmentSpec chosen = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _activeSegments.Count; i++)
        {
            var seg = _activeSegments[i];
            if (seg == null) continue;

            var provider = seg.GetComponentInChildren<CameraBoundsProvider>(true);
            if (provider == null || provider.Poly == null) continue;

            if (!provider.Poly.OverlapPoint(p)) continue;

            float d = (seg.transform.position - player.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                chosen = seg;
            }
        }

        return chosen;
    }

    // Next = menor StartPos.x maior que o StartPos.x do current
    private SegmentSpec FindNextSegment(SegmentSpec current)
    {
        if (current == null) return null;

        float curX = current.StartPos.x;
        SegmentSpec best = null;
        float bestX = float.MaxValue;

        for (int i = 0; i < _activeSegments.Count; i++)
        {
            var seg = _activeSegments[i];
            if (seg == null || seg == current) continue;

            float x = seg.StartPos.x;
            if (x <= curX) continue;

            if (x < bestX)
            {
                bestX = x;
                best = seg;
            }
        }

        return best;
    }

    // Prev = maior StartPos.x menor que o StartPos.x do current
    private SegmentSpec FindPrevSegment(SegmentSpec current)
    {
        if (current == null) return null;

        float curX = current.StartPos.x;
        SegmentSpec best = null;
        float bestX = float.MinValue;

        for (int i = 0; i < _activeSegments.Count; i++)
        {
            var seg = _activeSegments[i];
            if (seg == null || seg == current) continue;

            float x = seg.StartPos.x;
            if (x >= curX) continue;

            if (x > bestX)
            {
                bestX = x;
                best = seg;
            }
        }

        return best;
    }

    private void ApplyBounds(SegmentSpec prev, SegmentSpec cur, SegmentSpec next)
    {
        if (_appliedPrev == prev && _appliedCur == cur && _appliedNext == next)
            return;

        var sources = new List<PolygonCollider2D>(3);

        var p = GetPoly(prev);
        if (p != null) sources.Add(p);

        var c = GetPoly(cur);
        if (c != null) sources.Add(c);

        var n = GetPoly(next);
        if (n != null) sources.Add(n);

        boundsBuilder.SetSources(sources);

        _appliedPrev = prev;
        _appliedCur = cur;
        _appliedNext = next;

        if (logChanges)
        {
            string sPrev = prev != null ? prev.name : "(null)";
            string sCur = cur != null ? cur.name : "(null)";
            string sNext = next != null ? next.name : "(null)";
            Debug.Log($"[CameraBoundsManager] RuntimeBounds = {sPrev} + {sCur} + {sNext}");
        }
    }

    private PolygonCollider2D GetPoly(SegmentSpec seg)
    {
        if (seg == null) return null;
        var provider = seg.GetComponentInChildren<CameraBoundsProvider>(true);
        return provider != null ? provider.Poly : null;
    }

    private void HandleCheckpointReached(int stage, float pendingPercent)
    {
        // apenas reforço: se checkpoint abriu e estamos num End não liberado, segura
        if (IsInfinityMode() && _current != null && _current.tag == SegmentTag.End &&
            !_releasedInfinityEnds.Contains(_current))
        {
            _infinityEndHold = _current;
        }
    }

    private void HandleStageChanged()
    {
        // ✅ No Infinity, quando o player clica Continue:
        // RunManager seta IsInfinityCheckpointPending=false e incrementa stage
        // Aqui marcamos o End atual como "liberado".
        if (!IsInfinityMode() || RunManager.I == null) return;

        if (!RunManager.I.IsInfinityCheckpointPending)
        {
            if (_infinityEndHold != null)
            {
                _releasedInfinityEnds.Add(_infinityEndHold);
                _infinityEndHold = null;
            }
            else if (_current != null && _current.tag == SegmentTag.End)
            {
                // fallback: se por timing o hold não estava setado
                _releasedInfinityEnds.Add(_current);
            }
        }
    }

    // API pro streaming
    public void RegisterSegment(SegmentSpec seg)
    {
        if (seg == null) return;
        if (!_activeSegments.Contains(seg))
            _activeSegments.Add(seg);
    }

    public void UnregisterSegment(SegmentSpec seg)
    {
        if (seg == null) return;

        _activeSegments.Remove(seg);

        if (_current == seg) _current = null;
        if (_locked == seg) _locked = null;
        if (_infinityEndHold == seg) _infinityEndHold = null;

        // ✅ importantíssimo pra não vazar memória quando despawna segment antigo
        _releasedInfinityEnds.Remove(seg);

        if (_appliedPrev == seg) _appliedPrev = null;
        if (_appliedCur == seg) _appliedCur = null;
        if (_appliedNext == seg) _appliedNext = null;
    }
}
