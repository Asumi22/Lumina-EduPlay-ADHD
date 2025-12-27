package com.example.atentoyactivo.ui;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.atentoyactivo.R;
import com.example.atentoyactivo.model.Usuario;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class UsuarioAdapter extends RecyclerView.Adapter<UsuarioAdapter.VH> {

    private final List<Usuario> usuarios = new ArrayList<>();
    private Map<String, Long> clamsMap;

    public UsuarioAdapter() { }

    public void setUsuarios(List<Usuario> lista) {
        usuarios.clear();
        if (lista != null) usuarios.addAll(lista);
        notifyDataSetChanged();
    }

    public void setClamsMap(Map<String, Long> map) {
        this.clamsMap = map;
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public VH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_usuario, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VH holder, int position) {
        Usuario u = usuarios.get(position);
        String nombre = (u.getNombre() != null && !u.getNombre().isEmpty()) ? u.getNombre() : holder.itemView.getContext().getString(R.string.placeholder_dash);
        holder.name.setText(nombre);

        if (u.getAnioNacimiento() > 0) {
            // usa recurso para formato "X años"
            holder.age.setText(holder.itemView.getContext().getString(R.string.years_format, u.getAnioNacimiento()));
        } else {
            holder.age.setText("");
        }

        long clams;
        if (clamsMap != null && u.getUid() != null && clamsMap.containsKey(u.getUid())) {
            Long v = clamsMap.get(u.getUid());
            clams = (v != null) ? v : 0L;
        } else {
            clams = u.getClams();
        }
        holder.clams.setText(String.valueOf(clams));

        String inicial = holder.itemView.getContext().getString(R.string.avatar_default);
        if (u.getNombre() != null && !u.getNombre().trim().isEmpty()) {
            inicial = u.getNombre().trim().substring(0, 1).toUpperCase();
        }
        holder.avatar.setText(inicial);
    }

    @Override
    public int getItemCount() { return usuarios.size(); }

    static class VH extends RecyclerView.ViewHolder {
        TextView avatar, name, age, clams;
        VH(@NonNull View itemView) {
            super(itemView);
            avatar = itemView.findViewById(R.id.tvAvatar);
            name = itemView.findViewById(R.id.tvUserName);
            age = itemView.findViewById(R.id.tvUserAge);
            clams = itemView.findViewById(R.id.tvClams);
        }
    }
}
