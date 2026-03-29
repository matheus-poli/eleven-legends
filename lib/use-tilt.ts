"use client";

import { useRef, useCallback } from "react";

/**
 * Hook for DaisyUI-style 3D tilt effect on hover.
 * Returns ref to attach to the element + handlers.
 *
 * Usage:
 * ```tsx
 * const { ref, handlers } = useTilt();
 * <div ref={ref} {...handlers} className="card-3d">...</div>
 * ```
 */
export function useTilt(maxTilt = 8, scale = 1.04) {
  const ref = useRef<HTMLDivElement>(null);

  const onMouseMove = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      const el = ref.current;
      if (!el) return;

      const rect = el.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width;
      const y = (e.clientY - rect.top) / rect.height;

      const tiltX = (0.5 - y) * maxTilt;
      const tiltY = (x - 0.5) * maxTilt;

      el.style.transform = `perspective(800px) rotateX(${tiltX}deg) rotateY(${tiltY}deg) scale(${scale})`;
    },
    [maxTilt, scale],
  );

  const onMouseLeave = useCallback(() => {
    const el = ref.current;
    if (!el) return;
    el.style.transform = "perspective(800px) rotateX(0deg) rotateY(0deg) scale(1)";
  }, []);

  return {
    ref,
    handlers: {
      onMouseMove,
      onMouseLeave,
    },
  };
}
