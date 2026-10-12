using UnityEngine;

public abstract class Base_Item : MonoBehaviour
{
    public Transform target { get; set; } = null;
    protected bool claimed;

    protected virtual void OnEnable() { claimed = false; target = null; }

    protected virtual void OnTriggerStay2D(Collider2D col)
    {
        // A pickup deferred by a shared pause must still work if the player did
        // not leave/re-enter its trigger after both upgrade choices completed.
        if (LanSession.Active) OnTriggerEnter2D(col);
    }

    void Update()
    {
        if (!LanSession.Active || !LanSession.Instance.CanSimulate || target == null) return;
        var player = target.GetComponent<PlayerController>();
        if (player == null || LanSession.Instance.IsDown(player)) { target = null; return; }
        transform.position = Vector3.MoveTowards(transform.position, target.position, 10f * Time.deltaTime);
    }

    public abstract void OnItemEvent(PlayerStat player);

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            if (LanSession.Active && (claimed || !LanSession.Instance.CanSimulate || LanSession.Instance.IsDown(col.GetComponent<PlayerController>()))) return;
            claimed = true;
            PlayerStat playerStat = col.GetComponent<PlayerStat>();
            OnItemEvent(playerStat);
            target = null;
            Managers.Resource.Destroy(gameObject);
        }
    }
}
