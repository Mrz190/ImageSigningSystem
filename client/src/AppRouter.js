import React, { useEffect, useState } from "react";
import { Routes, Route, useNavigate, useLocation } from "react-router-dom";
import AuthForm from "./AuthForm";
import HomePage from "./HomePage";
import Users from "./Users";
import IndexPage from "./IndexPage";

function AppRouter() {
    const navigate = useNavigate();
    const location = useLocation();
    const userPasswordHash = sessionStorage.getItem("userPasswordHash");
    const role = sessionStorage.getItem("role");

    useEffect(() => {
        if (userPasswordHash && location.pathname === "/auth") {
            navigate("/home", { replace: true });
        }
        else if (!userPasswordHash && location.pathname === "/auth") {
            navigate("/auth", { replace: true });
        }
        else if (userPasswordHash && location.pathname === "/home"){
            navigate("/home", { replace: true });
        }
        else if (location.pathname === "/") {
            navigate("/", { replace: true });
        }
        else if (userPasswordHash && role === "Admin" && location.pathname === "/users"){
            navigate("/users", { replace: true });

        }
        else if (userPasswordHash && location.pathname !== "/auth") {
            navigate("/home", { replace: true });
        }
    }, [location.pathname, navigate]);

    return (
        <Routes>
            <Route path="/" element={<IndexPage />} />
            <Route path="/home" element={<HomePage />} />
            <Route path="/auth" element={<AuthForm />} />
            <Route path="/users" element={<Users />} />
        </Routes>
    );
}

export default AppRouter;
