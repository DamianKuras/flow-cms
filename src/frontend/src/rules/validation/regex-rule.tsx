import { useState } from "react";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { registerValidationRule } from "../../registry/validation-rule-registry";

const PRESETS: { label: string; pattern: string; hint: string }[] = [
  { label: "Email", pattern: "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", hint: "user@example.com" },
  { label: "URL", pattern: "^https?://[^\\s/$.?#].[^\\s]*$", hint: "https://example.com" },
  { label: "Numbers only", pattern: "^[0-9]+$", hint: "12345" },
  { label: "Letters only", pattern: "^[a-zA-Z]+$", hint: "hello" },
  { label: "Slug", pattern: "^[a-z0-9]+(?:-[a-z0-9]+)*$", hint: "my-slug-123" },
  { label: "No special characters", pattern: "^[a-zA-Z0-9\\s]+$", hint: "Hello World 123" },
];

registerValidationRule("RegexRule", {
  label: "Pattern (Regex)",
  supportedTypes: ["Text"],
  validate: (value, params) => {
    if (typeof value !== "string") return "Value must be a string";
    try {
      if (!new RegExp(params["regex"]).test(value)) {
        return `Value does not match pattern '${params["regex"]}'`;
      }
    } catch {
      return "Invalid pattern configuration";
    }
    return null;
  },
  HintComponent: ({ params }) => {
    const preset = PRESETS.find((p) => p.pattern === params["regex"]);
    return (
      <p className="text-xs">
        Must match pattern:{" "}
        {preset ? (
          <span className="font-medium">{preset.label}</span>
        ) : (
          <code>{params["regex"]}</code>
        )}
      </p>
    );
  },
  ConfigComponent: RegexConfig,
});

export function RegexConfig({
  value,
  onChange,
}: {
  value: Record<string, unknown>;
  onChange: (v: Record<string, unknown>) => void;
}) {
  const [preview, setPreview] = useState("");
  const pattern = (value["regex"] as string) ?? "";

  let patternError = false;
  let previewMatch: boolean | null = null;
  if (pattern) {
    try {
      new RegExp(pattern);
      if (preview) previewMatch = new RegExp(pattern).test(preview);
    } catch {
      patternError = true;
    }
  }

  return (
    <div className="space-y-3">
      <div className="space-y-1">
        <Label>Preset</Label>
        <Select
          value={PRESETS.find((p) => p.pattern === pattern)?.label ?? ""}
          onValueChange={(label) => {
            const preset = PRESETS.find((p) => p.label === label);
            if (preset) onChange({ ...value, regex: preset.pattern });
          }}
        >
          <SelectTrigger>
            <SelectValue placeholder="Choose a preset or write a custom pattern below" />
          </SelectTrigger>
          <SelectContent>
            {PRESETS.map((p) => (
              <SelectItem key={p.label} value={p.label}>
                {p.label}
                <span className="ml-2 text-xs text-muted-foreground">
                  e.g. {p.hint}
                </span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1">
        <Label>Pattern</Label>
        <Input
          type="text"
          value={pattern}
          onChange={(e) => onChange({ ...value, regex: e.target.value })}
          placeholder="e.g. ^[a-z]+$"
          className={patternError ? "border-destructive" : ""}
        />
        {patternError && (
          <p className="text-xs text-destructive">Invalid regular expression</p>
        )}
      </div>

      {pattern && !patternError && (
        <div className="space-y-1">
          <Label>Test a value</Label>
          <div className="flex gap-2 items-center">
            <Input
              type="text"
              value={preview}
              onChange={(e) => setPreview(e.target.value)}
              placeholder="Type a value to test the pattern..."
            />
            {preview && (
              <span
                className={
                  previewMatch
                    ? "text-sm text-green-600 font-medium whitespace-nowrap"
                    : "text-sm text-destructive font-medium whitespace-nowrap"
                }
              >
                {previewMatch ? "Match" : "No match"}
              </span>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
