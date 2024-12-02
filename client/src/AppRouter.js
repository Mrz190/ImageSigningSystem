import React, { useEffect, useState } from "react";
import { Routes, Route, useNavigate, useLocation } from "react-router-dom";
import AuthForm from "./AuthForm";
import HomePage from "./HomePage";
import Users from "./Users";
import IndexPage from "./IndexPage";

function AppRouter() {
    const navigate = useNavigate();
    const location = useLocation();
    const [isAuthenticated, setIsAuthenticated] = useState(false);

    useEffect(() => {
        const userPasswordHash = sessionStorage.getItem("userPasswordHash");
        if (userPasswordHash) {
            setIsAuthenticated(true);
        }
    }, []);

    useEffect(() => {
        if (isAuthenticated && location.pathname === "/auth") {
            navigate("/home", { replace: true });
        }
        else if (!isAuthenticated && location.pathname !== "/auth") {
            navigate("/", { replace: true });
        }
        else if (location.pathname === "/") {
            navigate("/", { replace: true });
        }
    }, [isAuthenticated, location.pathname, navigate]);

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
