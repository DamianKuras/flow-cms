import * as React from "react";

import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Label } from "@/components/ui/label";

import { useContentTypeSummaries } from "@/hooks/use-content-type-summaries";
import { useContentType } from "@/hooks/use-content-type";
import { useContent } from "@/hooks/use-content";
import type { ResourceType } from "@/hooks/use-roles";

interface ResourcePickerProps {
  resourceType: ResourceType;
  value: string | null;
  onChange: (id: string | null) => void;
}

// ── Single-level combobox ─────────────────────────────────────────────────────

interface ComboboxProps {
  placeholder: string;
  items: { id: string; label: string }[];
  value: string | null;
  onChange: (id: string | null) => void;
  loading?: boolean;
}

function Combobox({ placeholder, items, value, onChange, loading }: ComboboxProps) {
  const [open, setOpen] = React.useState(false);
  const selected = items.find((i) => i.id === value);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className="w-full justify-start font-normal truncate">
          {loading
            ? "Loading…"
            : selected
              ? selected.label
              : placeholder}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search…" />
          <CommandList>
            <CommandEmpty>No results found.</CommandEmpty>
            <CommandGroup>
              {items.map((item) => (
                <CommandItem
                  key={item.id}
                  value={item.label}
                  onSelect={() => {
                    onChange(item.id === value ? null : item.id);
                    setOpen(false);
                  }}
                >
                  {item.label}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

// ── Content-type picker ───────────────────────────────────────────────────────

function ContentTypePicker({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (id: string | null) => void;
}) {
  const { data, isLoading } = useContentTypeSummaries();
  const items = (data ?? []).map((s) => ({ id: s.name, label: s.name }));

  return (
    <Combobox
      placeholder="Pick a content type…"
      items={items}
      value={value}
      onChange={onChange}
      loading={isLoading}
    />
  );
}

// ── Content-item picker (needs content type selected first) ───────────────────

function ContentItemPicker({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (id: string | null) => void;
}) {
  const [contentTypeId, setContentTypeId] = React.useState<string | null>(null);
  const { data, isLoading } = useContent(contentTypeId ?? "");

  const items =
    data?.Content.map((ci) => ({ id: ci.id, label: ci.title })) ?? [];

  return (
    <div className="space-y-2">
      <div>
        <Label className="text-xs text-muted-foreground">Filter by content type</Label>
        <ContentTypePicker
          value={contentTypeId}
          onChange={(id) => {
            setContentTypeId(id);
            onChange(null); // reset item when type changes
          }}
        />
      </div>
      <div>
        <Label className="text-xs text-muted-foreground">Content item</Label>
        <Combobox
          placeholder={contentTypeId ? "Pick a content item…" : "Select a type first"}
          items={items}
          value={value}
          onChange={onChange}
          loading={isLoading && !!contentTypeId}
        />
      </div>
    </div>
  );
}

// ── Field picker (needs content type selected first) ──────────────────────────

function FieldPicker({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (id: string | null) => void;
}) {
  const [contentTypeId, setContentTypeId] = React.useState<string | null>(null);
  const { data, isLoading } = useContentType(contentTypeId ?? "");

  const items =
    data?.contentType.fields.map((f) => ({ id: f.id, label: f.name })) ?? [];

  return (
    <div className="space-y-2">
      <div>
        <Label className="text-xs text-muted-foreground">Filter by content type</Label>
        <ContentTypePicker
          value={contentTypeId}
          onChange={(id) => {
            setContentTypeId(id);
            onChange(null);
          }}
        />
      </div>
      <div>
        <Label className="text-xs text-muted-foreground">Field</Label>
        <Combobox
          placeholder={contentTypeId ? "Pick a field…" : "Select a type first"}
          items={items}
          value={value}
          onChange={onChange}
          loading={isLoading && !!contentTypeId}
        />
      </div>
    </div>
  );
}

// ── Main export ───────────────────────────────────────────────────────────────

export function ResourcePicker({ resourceType, value, onChange }: ResourcePickerProps) {
  // Reset value when resource type changes
  const prevResourceType = React.useRef(resourceType);
  React.useEffect(() => {
    if (prevResourceType.current !== resourceType) {
      prevResourceType.current = resourceType;
      onChange(null);
    }
  }, [resourceType, onChange]);

  if (resourceType === "contentType") {
    return <ContentTypePicker value={value} onChange={onChange} />;
  }
  if (resourceType === "contentItem") {
    return <ContentItemPicker value={value} onChange={onChange} />;
  }
  if (resourceType === "field") {
    return <FieldPicker value={value} onChange={onChange} />;
  }
  return null;
}
