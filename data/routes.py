import csv
import sys
from pathlib import Path

def convert(csv_file, sql_file, zone_id):
    nodes = {}
    edges = set()

    with open(csv_file, encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)

        for row in reader:
            node_id = int(row["Id"])
            x = int(row["X"])
            y = int(row["Y"])

            if node_id in nodes:
                raise ValueError(f"Duplicate node: {node_id}")

            neighbors = [
                int(v.strip())
                for v in row["Neighbors"].split(",")
                if v.strip()
            ]

            nodes[node_id] = (x, y, neighbors)

    # Checking neighbors and forming unique edges
    for node_id, (_, _, neighbors) in nodes.items():
        for neighbor in neighbors:
            if neighbor not in nodes:
                raise ValueError(
                    f"Node {node_id}: unknown neighbor {neighbor}"
                )

            if neighbor == node_id:
                raise ValueError(
                    f"Node {node_id}: self-reference"
                )

            # Undirected edge
            edge = tuple(sorted((node_id, neighbor)))
            edges.add(edge)

    lines = [
        "SET XACT_ABORT ON;",
        "BEGIN TRANSACTION;",
        "",
        "-- Nodes",
    ]

    for node_id, (x, y, _) in sorted(nodes.items()):
        lines.append(
            "INSERT INTO dbo.NpcRouteNode "
            "(ZoneId, Id, X, Y) "
            f"VALUES ({zone_id}, {node_id}, {x}, {y});"
        )

    lines.extend(["", "-- Edges"])

    for a, b in sorted(edges):
        lines.append(
            "INSERT INTO dbo.NpcRouteEdge "
            "(ZoneId, FromNodeId, ToNodeId) "
            f"VALUES ({zone_id}, {a}, {b});"
        )

    lines.extend(["", "COMMIT TRANSACTION;", ""])

    Path(sql_file).write_text(
        "\n".join(lines),
        encoding="utf-8"
    )

    print(f"Nodes: {len(nodes)}")
    print(f"Edges: {len(edges)}")
    print(f"Generated: {sql_file}")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("Usage: python routes.py input.csv output.sql zone_id")
        sys.exit(1)

    convert(sys.argv[1], sys.argv[2], int(sys.argv[3]))
