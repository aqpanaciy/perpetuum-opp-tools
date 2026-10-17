CREATE TABLE dbo.NpcRouteNode
(
    ZoneId INT NOT NULL,
    Id     INT NOT NULL,
    X      INT NOT NULL,
    Y      INT NOT NULL,

    CONSTRAINT PK_NpcRouteNode
        PRIMARY KEY (ZoneId, Id)
);

CREATE TABLE dbo.NpcRouteEdge
(
    ZoneId     INT NOT NULL,
    FromNodeId INT NOT NULL,
    ToNodeId   INT NOT NULL,

    CONSTRAINT PK_NpcRouteEdge
        PRIMARY KEY (ZoneId, FromNodeId, ToNodeId),

    CONSTRAINT FK_NpcRouteEdge_From
        FOREIGN KEY (ZoneId, FromNodeId)
        REFERENCES dbo.NpcRouteNode (ZoneId, Id),

    CONSTRAINT FK_NpcRouteEdge_To
        FOREIGN KEY (ZoneId, ToNodeId)
        REFERENCES dbo.NpcRouteNode (ZoneId, Id),

    CONSTRAINT CK_NpcRouteEdge_Order
        CHECK (FromNodeId < ToNodeId)
);
