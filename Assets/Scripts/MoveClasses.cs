
public struct Point
{
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    private int x;
    private int y;

    public int X
    {
        get { return x; }
        set { x = value; }
    }

    public int Y
    {
        get { return y; }
        set { y = value; }
    }

    public static bool operator ==(Point obj1, Point obj2)
    {
        return obj1.X == obj2.X && obj1.Y == obj2.Y;
    }
    public static bool operator !=(Point obj1, Point obj2)
    {
        return obj1.X != obj2.X || obj1.Y != obj2.Y;
    }
    public bool Equals(Point obj)
    {
        return X == obj.X && Y == obj.Y;
    }
    public override bool Equals(object obj)
    {
        return obj is Point && Equals((Point)obj);
    }
    public override int GetHashCode()
    {
        return (X << 2) ^ Y;
    }
}

public struct Attack
{
    public Attack(Point capturedPieceCoords, string capturedPieceName, uint capturedPieceID)
    {
        this.capturedPieceCoords = capturedPieceCoords;
        this.capturedPieceName = capturedPieceName;
        this.capturedPieceID = capturedPieceID;
    }

    private Point capturedPieceCoords;
    private string capturedPieceName;
    private uint capturedPieceID;

    public Point CapturedPieceCoords
    {
        get { return capturedPieceCoords; }
        set { capturedPieceCoords = value; }
    }

    public string CapturedPieceName
    {
        get { return capturedPieceName; }
        set { capturedPieceName = value; }
    }
    public uint CapturedPieceID
    {
        get { return capturedPieceID; }
        set { capturedPieceID = value; }
    }
    public bool Equals(Attack obj)
    {
        return capturedPieceCoords == obj.capturedPieceCoords && capturedPieceName == obj.capturedPieceName && capturedPieceID == obj.capturedPieceID;
    }
    public override bool Equals(object obj)
    {
        return obj is Attack && Equals((Attack)obj);
    }
    public static bool operator ==(Attack obj1, Attack obj2)
    {
        return obj1.capturedPieceCoords == obj2.capturedPieceCoords && obj1.capturedPieceName == obj2.capturedPieceName && obj1.capturedPieceID == obj2.capturedPieceID;
    }
    public static bool operator !=(Attack obj1, Attack obj2)
    {
        return obj1.capturedPieceCoords != obj2.capturedPieceCoords || obj1.capturedPieceName != obj2.capturedPieceName || obj1.capturedPieceID != obj2.capturedPieceID;
    }
}

public struct Castle
{
    public Castle(Point rookFrom, Point rookTo, bool isShortCastle)
    {
        this.rookFrom = rookFrom;
        this.rookTo = rookTo;
        this.isShortCastle = isShortCastle;
    }

    private Point rookFrom;
    private Point rookTo;
    private bool isShortCastle;

    public Point RookFrom
    {
        get { return rookFrom; }
        set { rookFrom = value; }
    }
    public Point RookTo
    {
        get { return rookTo; }
        set { rookTo = value; }
    }
    public bool IsShortCastle
    {
        get { return isShortCastle; }
        set { isShortCastle = value; }
    }
    public bool Equals(Castle obj)
    {
        return rookFrom == obj.rookFrom && rookTo == obj.rookTo && isShortCastle == obj.isShortCastle;
    }
    public override bool Equals(object obj)
    {
        return obj is Castle && Equals((Castle)obj);
    }
    public static bool operator ==(Castle obj1, Castle obj2)
    {
        return obj1.rookFrom == obj2.rookFrom && obj1.rookTo == obj2.rookTo && obj1.isShortCastle == obj2.isShortCastle;
    }
    public static bool operator !=(Castle obj1, Castle obj2)
    {
        return obj1.rookFrom != obj2.rookFrom || obj1.rookTo != obj2.rookTo || obj1.isShortCastle != obj2.isShortCastle;
    }
}

public struct Promote
{
    public Promote(string promotedTo)
    {
        this.promotedTo = promotedTo;
    }

    private string promotedTo;

    public string PromotedTo
    {
        get { return promotedTo; }
        set { promotedTo = value; }
    }
    public bool Equals(Promote obj)
    {
        return promotedTo == obj.promotedTo;
    }
    public override bool Equals(object obj)
    {
        return obj is Promote && Equals((Promote)obj);
    }
    public static bool operator ==(Promote obj1, Promote obj2)
    {
        return obj1.promotedTo == obj2.promotedTo;
    }
    public static bool operator !=(Promote obj1, Promote obj2)
    {
        return obj1.promotedTo != obj2.promotedTo;
    }
}

public struct Check
{
    public Check(bool isChecked, bool isMated, bool isDrawn)
    {
        this.isChecked = isChecked;
        this.isMated = isMated;
        this.isDrawn = isDrawn;
    }

    private bool isChecked;
    private bool isMated;
    private bool isDrawn;

    public bool IsChecked
    {
        get { return isChecked; }
        set { isChecked = value; }
    }
    public bool IsMated
    {
        get { return isMated; }
        set { isMated = value; }
    }
    public bool IsDrawn
    {
        get { return isDrawn; }
        set { isDrawn = value; }
    }
    public bool Equals(Check obj)
    {
        return isChecked == obj.isChecked && isMated == obj.isMated && isDrawn == obj.isDrawn;
    }
    public override bool Equals(object obj)
    {
        return obj is Check && Equals((Check)obj);
    }
    public static bool operator ==(Check obj1, Check obj2)
    {
        return obj1.isChecked == obj2.isChecked && obj1.isMated == obj2.isMated && obj1.isDrawn == obj2.isDrawn;
    }
    public static bool operator !=(Check obj1, Check obj2)
    {
        return obj1.isChecked != obj2.isChecked || obj1.isMated != obj2.isMated || obj1.isDrawn != obj2.isDrawn;
    }
}

public struct Move
{
    public Move(Point from, Point to, string movingPieceName, Attack? attack, Castle? castle, Promote? promote, Check? check)
    {
        this.from = from;
        this.to = to;
        this.movingPieceName = movingPieceName;
        this.attack = attack;
        this.castle = castle;
        this.promote = promote;
        this.check = check;
    }

    private Point from;
    private Point to;
    private string movingPieceName;
    private Attack? attack;
    private Castle? castle;
    private Promote? promote;
    private Check? check;

    public Point From
    {
        get { return from; }
        set { from = value; }
    }
    public Point To
    {
        get { return to; }
        set { to = value; }
    }
    public string MovingPieceName
    {
        get { return movingPieceName; }
        set { movingPieceName = value; }
    }
    public Attack? Attack
    {
        get { return attack; }
        set { attack = value; }
    }
    public Castle? Castle
    {
        get { return castle; }
        set { castle = value; }
    }
    public Promote? Promote
    {
        get { return promote; }
        set { promote = value; }
    }
    public Check? Check
    {
        get { return check; }
        set { check = value; }
    }


    public bool Equals(Move obj)
    {
        return from == obj.from && to == obj.to && movingPieceName == obj.movingPieceName && attack == obj.attack && castle == obj.castle && promote == obj.promote && check == obj.check;
    }
    public override bool Equals(object obj)
    {
        return obj is Move && Equals((Move)obj);
    }
    public static bool operator ==(Move obj1, Move obj2)
    {
        return obj1.from == obj2.from && obj1.to == obj2.to && obj1.movingPieceName == obj2.movingPieceName && obj1.attack == obj2.attack && obj1.castle == obj2.castle && obj1.promote == obj2.promote && obj1.check == obj2.check;
    }
    public static bool operator !=(Move obj1, Move obj2)
    {
        return obj1.from != obj2.from || obj1.to != obj2.to || obj1.movingPieceName != obj2.movingPieceName || obj1.attack != obj2.attack || obj1.castle != obj2.castle || obj1.promote != obj2.promote || obj1.check != obj2.check;
    }
}

public struct MoveUpdater
{
    public MoveUpdater(uint id, bool attackable, bool protectable = false)
    {
        this.id = id;
        this.attackable = attackable;
        this.protectable = protectable;
    }

    public uint id;
    public bool attackable;
    public bool protectable;

    public static bool operator ==(MoveUpdater obj1, MoveUpdater obj2)
    {
        return obj1.id == obj2.id && obj1.attackable == obj2.attackable && obj1.protectable == obj2.protectable;
    }
    public static bool operator !=(MoveUpdater obj1, MoveUpdater obj2)
    {
        return obj1.id != obj2.id || obj1.attackable != obj2.attackable || obj1.protectable != obj2.protectable;
    }
    public bool Equals(MoveUpdater obj)
    {
        return id == obj.id && attackable == obj.attackable && protectable == obj.protectable;
    }
    public override bool Equals(object obj)
    {
        return obj is MoveUpdater && Equals((MoveUpdater)obj);
    }
}