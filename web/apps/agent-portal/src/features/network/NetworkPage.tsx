"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import {
  Badge,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  SelectField,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";

export function NetworkPage() {
  const scope = useAgentScope();
  const [depth, setDepth] = useState(3);
  const [view, setView] = useState<"tree" | "table" | "children" | "ancestors">(
    "tree",
  );
  const tree = useApiQuery(
    (api, signal) =>
      agentApi.tree(api, scope.organizationId, scope.agentId, depth, signal),
    [scope.organizationId, scope.agentId, depth],
    scope.isReady,
  );
  const downline = useApiQuery(
    (api, signal) =>
      agentApi.downline(
        api,
        scope.organizationId,
        scope.agentId,
        depth,
        signal,
      ),
    [scope.organizationId, scope.agentId, depth],
    scope.isReady && view === "table",
  );
  const children = useApiQuery(
    (api, signal) =>
      agentApi.children(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady && view === "children",
  );
  const ancestors = useApiQuery(
    (api, signal) =>
      agentApi.ancestors(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady && view === "ancestors",
  );
  const legs = useApiQuery(
    (api, signal) =>
      agentApi.legSummary(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const recruits = useApiQuery(
    (api, signal) =>
      agentApi.recruits(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  if (tree.isLoading || legs.isLoading) return <Loading />;
  if (tree.error) return <Failure error={tree.error} retry={tree.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Network"
        description="Your authorized placement network. Agent identity comes from the current session."
      />
      <div className="toolbar">
        <SelectField
          id="network-depth"
          label="Depth"
          value={depth}
          onChange={(event) => setDepth(Number(event.target.value))}
        >
          {[2, 3, 4, 5, 10].map((value) => (
            <option key={value} value={value}>
              {value} levels
            </option>
          ))}
        </SelectField>
        <div>
          <Button
            variant={view === "tree" ? "primary" : "secondary"}
            onClick={() => setView("tree")}
          >
            Tree
          </Button>{" "}
          <Button
            variant={view === "table" ? "primary" : "secondary"}
            onClick={() => setView("table")}
          >
            Table
          </Button>{" "}
          <Button
            variant={view === "children" ? "primary" : "secondary"}
            onClick={() => setView("children")}
          >
            Direct children
          </Button>{" "}
          <Button
            variant={view === "ancestors" ? "primary" : "secondary"}
            onClick={() => setView("ancestors")}
          >
            Placement ancestors
          </Button>
        </div>
      </div>
      <div className="metric-grid">
        <Card>
          <span>Left network</span>
          <strong className="metric-value">
            {legs.data?.leftAgentCount ?? 0}
          </strong>
        </Card>
        <Card>
          <span>Right network</span>
          <strong className="metric-value">
            {legs.data?.rightAgentCount ?? 0}
          </strong>
        </Card>
        <Card>
          <span>Direct recruits</span>
          <strong className="metric-value">{recruits.data?.length ?? 0}</strong>
        </Card>
      </div>
      {view === "tree" ? (
        !tree.data?.length ? (
          <EmptyState
            title="No placement network"
            description="Your placement will appear after it is completed."
          />
        ) : (
          <div className="network-tree" role="tree">
            {tree.data.map((node) => (
              <Card
                key={node.agentId}
                role="treeitem"
                aria-level={node.depth + 1}
                style={{
                  marginInlineStart: `${Math.min(node.depth, 5) * 1.5}rem`,
                }}
              >
                <strong>{node.agentCode}</strong>{" "}
                <Badge>
                  {node.side === 0
                    ? "Left"
                    : node.side === 1
                      ? "Right"
                      : "Root"}
                </Badge>
                <small>Status {node.status}</small>
              </Card>
            ))}
          </div>
        )
      ) : view === "table" && downline.isLoading ? (
        <Loading />
      ) : view === "table" && downline.error ? (
        <Failure error={downline.error} retry={downline.reload} />
      ) : view === "table" ? (
        <DataTable
          caption="Downline network"
          rows={downline.data ?? []}
          rowKey={(row) => row.agentId}
          columns={[
            { key: "agent", header: "Agent", cell: (row) => row.agentCode },
            { key: "depth", header: "Depth", cell: (row) => row.depth },
            {
              key: "leg",
              header: "First leg",
              cell: (row) => (row.firstLeg === 0 ? "Left" : "Right"),
            },
            { key: "status", header: "Status", cell: (row) => row.status },
            {
              key: "joined",
              header: "Joined",
              cell: (row) => date(row.joinedAt),
            },
          ]}
        />
      ) : view === "children" && children.isLoading ? (
        <Loading />
      ) : view === "children" && children.error ? (
        <Failure error={children.error} retry={children.reload} />
      ) : view === "children" ? (
        !children.data?.length ? (
          <EmptyState
            title="No direct placement children"
            description="Agents placed immediately below you will appear here."
          />
        ) : (
          <DataTable
            caption="Direct placement children"
            rows={children.data}
            rowKey={(row) => row.agentId}
            columns={[
              { key: "agent", header: "Agent", cell: (row) => row.agentCode },
              {
                key: "side",
                header: "Side",
                cell: (row) => placementSide(row.side),
              },
              { key: "status", header: "Status", cell: (row) => row.status },
              {
                key: "joined",
                header: "Joined",
                cell: (row) => date(row.joinedAt),
              },
            ]}
          />
        )
      ) : ancestors.isLoading ? (
        <Loading />
      ) : ancestors.error ? (
        <Failure error={ancestors.error} retry={ancestors.reload} />
      ) : !ancestors.data?.length ? (
        <EmptyState
          title="No placement ancestors"
          description="Your upstream placement path will appear after placement."
        />
      ) : (
        <DataTable
          caption="Placement ancestors"
          rows={ancestors.data}
          rowKey={(row) => row.agentId}
          columns={[
            { key: "agent", header: "Agent", cell: (row) => row.agentCode },
            { key: "depth", header: "Levels above", cell: (row) => row.depth },
            {
              key: "leg",
              header: "Your descendant leg",
              cell: (row) => placementSide(row.descendantLeg),
            },
            { key: "status", header: "Status", cell: (row) => row.status },
          ]}
        />
      )}
    </div>
  );
}

function placementSide(side: number | null) {
  if (side === 0) return "Left";
  if (side === 1) return "Right";
  return "Not placed";
}
