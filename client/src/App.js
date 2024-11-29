import React from "react";
import { BrowserRouter as BrowserRouter } from "react-router-dom";
import "./styles/style.css";
import "./styles/reset.css";
import "./styles/global.css";
import "./styles/media.css";
import "./styles/fonts.css";
import AppRouter from "./AppRouter";

function App() {
  return (
    <BrowserRouter>
      <AppRouter/>
    </BrowserRouter>
  );
}

export default App;
