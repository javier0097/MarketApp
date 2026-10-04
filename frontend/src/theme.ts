import { createTheme, type MantineColorsTuple } from "@mantine/core";

// Brand tan #b89872 is shade 6
const tan: MantineColorsTuple = [
  "#f9f7f4",
  "#f2ece6",
  "#e8ded2",
  "#ddcebb",
  "#d2bda5",
  "#c5ab8b",
  "#b89872",
  "#9c8161",
  "#816a50",
  "#65543f",
];

// Brand green #233d24 is shade 7, the one primaryShade points to
const forest: MantineColorsTuple = [
  "#f2f3f2",
  "#e0e4e0",
  "#c6cdc6",
  "#a7b1a7",
  "#889689",
  "#697b6a",
  "#465c47",
  "#233d24",
  "#1c311d",
  "#152516",
];

const gray: MantineColorsTuple = [
  "#f3f1ef",
  "#ececeb",
  "#e5e5e5",
  "#dbdcdd",
  "#cccfd2",
  "#aeb3b7",
  "#8a8f93",
  "#525659",
  "#3f4244",
  "#2d2e2f",
];

const fontFamily = "'Atkinson Hyperlegible Next Variable', sans-serif";

export const theme = createTheme({
  fontFamily,
  headings: { fontFamily },
  colors: { tan, forest, gray },
  primaryColor: "forest",
  primaryShade: 7,
  spacing: {
    xs: "0.25rem",
    sm: "0.5rem",
    md: "1rem",
    lg: "1.5rem",
    xl: "2rem",
  },
});
