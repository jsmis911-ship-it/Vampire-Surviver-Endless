using UnityEngine;

/// <summary>One placed defense. All numbers are local to endless mode.</summary>
public sealed class EndlessBuilding : MonoBehaviour
{
    EndlessMode mode;
    SpriteRenderer sprite;
    float nextAttack;
    public int Kind { get; private set; }
    public int Level { get; private set; } = 1;
    public int Health { get; private set; }
    public int Cost { get; private set; }
    public int UpgradeCost { get { return 20 + Level * 15; } }
    public bool Selected { get; set; }

    public void Initialize(EndlessMode owner, int kind, Color color)
    {
        mode = owner;
        Kind = kind;
        Cost = new[] { 35, 20, 40, 55 }[kind];
        Health = kind == 1 ? 140 : 80;
        sprite = gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = EndlessVisual.Tower;
        sprite.color = color;
        sprite.sortingOrder = 4;
        transform.localScale = Vector3.one * 1.4f;
        if (kind == 1)
        {
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * .65f;
        }
    }

    void Update()
    {
        if (mode == null || mode.IsDefeated || Managers.gameStop || Time.time < nextAttack) return;
        if (Kind == 1) { nextAttack = Time.time + .7f; DamageNearbyWall(); return; }
        float range = Kind == 0 ? 5.8f : 4.2f;
        EnemyController target = mode.NearestEnemy(transform.position, range);
        if (target == null) return;
        int strength = Level + mode.TowerBonus;
        if (Kind == 0)
        {
            target.OnDamaged(8 + strength * 5);
            nextAttack = Time.time + Mathf.Max(.15f, .55f - strength * .025f);
        }
        else if (Kind == 2)
        {
            mode.DamageEnemies(transform.position, 3.1f, 2 + strength * 2, true);
            nextAttack = Time.time + 1.1f;
        }
        else
        {
            mode.DamageEnemies(transform.position, 3.8f, 11 + strength * 7);
            nextAttack = Time.time + 1.25f;
        }
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 5f) * 5f);
    }

    void DamageNearbyWall()
    {
        EnemyController target = mode.NearestEnemy(transform.position, .85f);
        if (target == null) return;
        Health -= Mathf.Max(1, target.GetComponent<EnemyStat>().Damage);
        if (Health <= 0) mode.SellDestroyed(this);
    }

    public void Upgrade()
    {
        if (Level >= 5 || !mode.Spend(UpgradeCost)) return;
        Level++;
        Health += 30;
        transform.localScale = Vector3.one * (1.4f + Level * .06f);
    }

    void OnDestroy()
    {
        if (Kind == 1 && EndlessDungeon.Active != null)
            EndlessDungeon.Active.SetBuildBlock(transform.position, false);
    }
}

/// <summary>Temporary slow without permanently changing the enemy's base speed.</summary>
public sealed class EndlessSlow : MonoBehaviour
{
    EnemyStat stat;
    float originalSpeed, expires;
    void Awake() { stat = GetComponent<EnemyStat>(); }
    public void Apply()
    {
        if (stat == null) return;
        if (expires < Time.time) originalSpeed = stat.MoveSpeed;
        stat.MoveSpeed = originalSpeed * .55f;
        expires = Time.time + 1.5f;
    }
    void Update()
    {
        if (expires <= 0 || Time.time < expires || stat == null) return;
        stat.MoveSpeed = originalSpeed;
        expires = 0;
    }
    void OnDisable()
    {
        if (expires > 0 && stat != null) stat.MoveSpeed = originalSpeed;
        expires = 0;
    }
}

/// <summary>Small pixel sprites generated once so the mode runs with the assets already in the project.</summary>
public static class EndlessVisual
{
    static Sprite crystal, tower, foundation;
    public static Sprite Crystal { get { return crystal ?? (crystal = Make(true)); } }
    public static Sprite Tower { get { return tower ?? (tower = Make(false)); } }
    public static Sprite Foundation { get { return foundation ?? (foundation = MakeFoundation()); } }

    static Sprite MakeFoundation()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - 15.5f) / 14f, dy = (y - 13f) / 9f;
            float d = dx * dx + dy * dy;
            pixels[y * size + x] = d > 1.1f ? new Color32(0,0,0,0) :
                d > .83f ? new Color32(24,31,43,255) :
                d > .57f ? new Color32(95,112,132,255) :
                new Color32(41,57,75,255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), 32f);
    }

    static Sprite Make(bool gem)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Color32 color = new Color32(0,0,0,0);
            if (gem)
            {
                bool baseRing = y < 9 && (x > 2 && x < 29);
                int half = y < 9 ? 0 : y < 24 ? 8 : Mathf.Max(1, 8 - (y - 24));
                bool body = y >= 9 && Mathf.Abs(x - 16) < half;
                if (baseRing) color = y > 5 ? new Color32(55,72,95,255) : new Color32(123,146,161,255);
                if (body) color = x < 16 ? new Color32(34,206,245,255) : new Color32(137,247,255,255);
                if (body && (x == 16 || y == 30)) color = new Color32(231,255,255,255);
            }
            else
            {
                bool baseBody = y < 8 && x > 2 && x < 29;
                bool turret = x > 8 && x < 24 && y >= 8 && y < 23;
                bool barrel = x > 16 && x < 31 && y >= 16 && y < 20;
                if (baseBody) color = new Color32(45,55,70,255);
                if (turret) color = new Color32(102,124,146,255);
                if (barrel) color = new Color32(39,47,62,255);
                if (turret && x > 12 && x < 19 && y > 12 && y < 20) color = new Color32(165,236,255,255);
            }
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), 32f);
    }
}
