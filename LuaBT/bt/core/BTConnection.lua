local BTConnection = bt.Class("BTConnection")
bt.BTConnection = BTConnection

function BTConnection:ctor()
    self.id = 0
    self.name = "BTConnection"
    self.sourceNode = nil
    self.targetNode = nil
    self.isDisabled = false
    self.status = bt.Status.Resting
end

function BTConnection:setActive(flag)
    if not self.isDisabled and not flag then
        self:reset(true)
    end
    self.isDisabled = not flag
end

function BTConnection:isActive()
    return not self.isDisabled
end

function BTConnection:create(id,source,target,isDisabled)
    if source == nil or target == nil then
        print("Can't Create a Connection without providing Source and Target Nodes")
        return nil
    end
    self.id = id
    self.isDisabled = isDisabled
    self.sourceNode = source
    self.targetNode = target
end

function BTConnection:execute(agent,blackboard)
    if not self:isActive() then
        return bt.Status.Resting
    end
    self.status = self.targetNode:execute(agent,blackboard)
    return self.status
end

function BTConnection:reset(recursively)
    if self.status == bt.Status.Resting then
        return
    end
    self.status = bt.Status.Resting
    if recursively then
        self.targetNode:reset(recursively)
    end
end