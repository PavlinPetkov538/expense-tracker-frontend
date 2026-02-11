import { createTheme } from "@mui/material/styles";

export const theme = createTheme({
  palette: {
    mode: "light",
    background: { default: "#ffffff", paper: "#ffffff" },
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: ["Inter", "system-ui", "Segoe UI", "Roboto", "Arial"].join(","),
  },
});
