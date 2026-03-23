import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

vi.mock("@/registry/validation-rule-registry", () => ({
  registerValidationRule: vi.fn(),
}));

import { MinimumLengthConfig } from "@/rules/validation/min-length-rule";

describe("MinimumLengthConfig", () => {
  // Regression: the component previously bound to `value.message` (a string input)
  // instead of `value["min-length"]` (a number input). This caused the backend to
  // receive parameters without the "min-length" key, throwing KeyNotFoundException.
  it("calls onChange with min-length key when user types a number", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<MinimumLengthConfig value={{}} onChange={onChange} />);

    const input = screen.getByRole("spinbutton");
    await user.clear(input);
    await user.type(input, "5");

    const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
    expect(lastCall["min-length"]).toBe(5);
  });

  it("does NOT produce a message key", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<MinimumLengthConfig value={{}} onChange={onChange} />);

    const input = screen.getByRole("spinbutton");
    await user.clear(input);
    await user.type(input, "10");

    const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
    expect(lastCall).not.toHaveProperty("message");
  });

  it("sets min-length to undefined when input is cleared", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <MinimumLengthConfig value={{ "min-length": 5 }} onChange={onChange} />,
    );

    const input = screen.getByRole("spinbutton");
    await user.clear(input);

    const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
    expect(lastCall["min-length"]).toBeUndefined();
  });

  it("renders a number input (not a text input)", () => {
    const onChange = vi.fn();
    render(<MinimumLengthConfig value={{}} onChange={onChange} />);

    const input = screen.getByRole("spinbutton");
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute("type", "number");
  });

  it("displays existing min-length value", () => {
    const onChange = vi.fn();
    render(
      <MinimumLengthConfig value={{ "min-length": 8 }} onChange={onChange} />,
    );

    const input = screen.getByRole("spinbutton") as HTMLInputElement;
    expect(input.value).toBe("8");
  });
});
