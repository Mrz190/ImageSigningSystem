import React, { useState, useEffect } from 'react';
import CryptoJS from 'crypto-js';
import config from './config';

const Images = () => {
  const [images, setImages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const role = localStorage.getItem('role');

  const getDigestAuthorizationHeader = async (method, uri) => {
    const local_HA1 = localStorage.getItem('userPasswordHash');
    const local_realm = localStorage.getItem('realm');
    const local_username = localStorage.getItem('username');

    if (!role || !local_HA1) {
      throw new Error('User is not authenticated or role is missing');
    }

    const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, { method: 'GET' });

    if (!nonceResponse.ok) {
      throw new Error('Failed to fetch nonce');
    }

    const nonceData = await nonceResponse.json();
    const nonce = nonceData.nonce;

    const qop = 'auth';
    const nc = '00000001';
    const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(local_HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);

    return `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${response}"`;
  };

  const fetchImages = async () => {
    if (loading) return;
    setLoading(true);

    try {
      const uri = getUriForRole(role);
      const authorizationHeader = await getDigestAuthorizationHeader('GET', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': authorizationHeader,
        },
      });
      debugger;
      if (!response.ok) {
        return;
      }

      const data = await response.json();
      setImages(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchImages();
  }, []);

  const getUriForRole = (role) => {
    switch (role) {
      case 'User':
        return '/User/get-user-images';
      case 'Support':
        return '/Support/get-support-images';
      case 'Admin':
        return '/Administrator/get-admin-images';
      default:
        throw new Error('Invalid role');
    }
  };

  const handleDownloadImage = async (id) => {
    try {
      setLoading(true);

      const uri = `/User/download/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('GET', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to download image');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `image-${id}.png`;
      link.click();
      window.URL.revokeObjectURL(url);

      fetchImages();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteImage = async (id) => {
    if (!window.confirm('Are you sure you want to delete this image?')) return;

    try {
      setLoading(true);

      const uri = `/User/delete-image/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('DELETE', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'DELETE',
        headers: {
          'Authorization': authorizationHeader,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete image');
      }

      setImages(images.filter((image) => image.id !== id));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSignImage = async (id) => {
    try {
      const uri = `/Administrator/sign/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('POST', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'POST',
        headers: {
          'Authorization': authorizationHeader,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to sign image');
      }

      fetchImages();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleRejectImage = async (id) => {
    try {
      const uri = `/Administrator/reject-signing/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('POST', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'POST',
        headers: {
          'Authorization': authorizationHeader,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to reject image');
      }

      fetchImages();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleAcceptImage = async (id) => {
    try {
      const uri = `/Support/request-signature/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('POST', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'POST',
        headers: {
          'Authorization': authorizationHeader,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to accept image');
      }

      fetchImages();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleSupportRejectImage = async (id) => {
    try {
      const uri = `/Support/reject-signing/${id}`;
      const authorizationHeader = await getDigestAuthorizationHeader('POST', uri);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'POST',
        headers: {
          'Authorization': authorizationHeader,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to reject image');
      }

      fetchImages();
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) {
    return <div className="loading">Loading...</div>;
  }

  if (error) {
    return <div className="error">Error occurred while loading data. Try later.</div>;
  }

  if (images.length === 0) {
    return <div className="no-images">No images found</div>;
  }

  if (role === "Admin") {
    return (
      <div className="images-container">
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Sign</th>
              <th>Reject</th>
              <th>Download</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>{image.imageName ? (image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName) : "No Name"}</td>
                <td>{image.status}</td>
                <td>
                  <button className="sign-btn" onClick={() => handleSignImage(image.id)}>Sign</button>
                </td>
                <td>
                  <button className="reject-btn" onClick={() => handleRejectImage(image.id)}>Reject</button>
                </td>
                <td>
                  <button className="download-btn" onClick={() => handleDownloadImage(image.id)}>Download</button>
                </td>
                <td>
                  <button className="view-btn" onClick={() => handleDownloadImage(image.id)}>View</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  } else if (role === "Support") {
    return (
      <div className="images-container">
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Accept</th>
              <th>Reject</th>
              <th>Download</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>{image.imageName ? (image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName) : "No Name"}</td>
                <td>{image.status}</td>
                <td>
                  <button className="accept-btn" onClick={() => handleAcceptImage(image.id)}>Accept</button>
                </td>
                <td>
                  <button className="reject-btn" onClick={() => handleSupportRejectImage(image.id)}>Reject</button>
                </td>
                <td>
                  <button className="download-btn" onClick={() => handleDownloadImage(image.id)}>Download</button>
                </td>
                <td>
                  <button className="view-btn" onClick={() => handleDownloadImage(image.id)}>View</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  return (
    <div className="images-container">
      <table className="images-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Download</th>
            <th>Delete</th>
            <th>View</th>
          </tr>
        </thead>
        <tbody>
          {images.map((image) => (
            <tr key={image.id}>
              <td>{image.imageName && image.imageName.length > 15 ? `${image.imageName.substring(0, 15)}...` : image.imageName || "No Name"}</td>
              <td>{image.status}</td>
              <td>
                <button className="download-btn" onClick={() => handleDownloadImage(image.id)}>Download</button>
              </td>
              <td>
                <button className="del-btn" onClick={() => handleDeleteImage(image.id)}>Delete</button>
              </td>
              <td>
                <button className="view-btn" onClick={() => handleDownloadImage(image.id)}>View</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default Images;
