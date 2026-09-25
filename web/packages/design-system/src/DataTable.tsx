import type { ReactNode } from "react";

export interface DataTableColumn<T> {
  key: string;
  header: string;
  cell(item: T): ReactNode;
  align?: "start" | "center" | "end";
}

export function DataTable<T>({
  caption,
  columns,
  rows,
  rowKey,
}: {
  caption: string;
  columns: readonly DataTableColumn<T>[];
  rows: readonly T[];
  rowKey(item: T): string;
}) {
  return (
    <div
      className="ds-table-scroll"
      tabIndex={0}
      role="region"
      aria-label={caption}
    >
      <table className="ds-table">
        <caption className="ds-sr-only">{caption}</caption>
        <thead>
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className={`ds-align-${column.align ?? "start"}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowKey(row)}>
              {columns.map((column) => (
                <td
                  key={column.key}
                  className={`ds-align-${column.align ?? "start"}`}
                >
                  {column.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
